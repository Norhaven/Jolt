using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using Jolt.Structure.Streaming;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jolt
{
    public class JoltTransformer<TContext> : IJsonTransformer<TContext> where TContext : IJsonContext
    {
        private readonly TContext _context;

        internal TContext Context => _context;
        
        public JoltTransformer(TContext context)
        {
            _context = context;

            _context.ReferenceResolver.Clear();
            _context.ReferenceResolver.RegisterMethods(_context.MethodRegistrations, _context.MethodContext);
        }

        public string? Transform(string json)
        {
            var source = _context.JsonTokenReader.Read(json);

            if (source is null)
            {
                throw new InvalidOperationException("The source document could not be read as valid JSON, please verify that your source document is valid.");
            }

            var transformer = _context.JsonTokenReader.Read(_context.JsonTransformer);

            if (transformer is null)
            {
                throw new InvalidOperationException("The transformer could not be read as valid JSON, please verify that your transformer is valid");
            }

            var transformation = EvaluationToken.From(transformer);

            return TransformToken(transformation, EvaluationScope.Empty.CreateClosureOver(source))?.ToString();
        }

        public void TransformSequence(Stream input, Stream output, StreamingOptions? options = default)
        {
            OpenStreamingTransformer(options, x => x.TransformSequence(input, output));
        }

        public void TransformLines(Stream input, Stream output, StreamingOptions? options = default)
        {
            OpenStreamingTransformer(options, x => x.TransformLines(input, output));
        }

        public void TransformLines(TextReader input, TextWriter output, StreamingOptions? options = default)
        {
            OpenStreamingTransformer(options, x => x.TransformLines(input, output));
        }

        public Task TransformLinesAsync(Stream input, Stream output, CancellationToken? cancellationToken = default, StreamingOptions? options = default)
        {
            return OpenStreamingTransformerAsync(options, async x => x.TransformLinesAsync(input, output, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
        }

        public Task TransformLinesAsync(TextReader input, TextWriter output, CancellationToken? cancellationToken = null, StreamingOptions? options = default)
        {
            return OpenStreamingTransformerAsync(options, async x => await x.TransformLinesAsync(input, output, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
        }

        public void TransformSequence(TextReader reader, TextWriter writer, StreamingOptions? options = default)
        {
            OpenStreamingTransformer(options, x => x.TransformSequence(reader, writer));
        }

        public Task TransformSequenceAsync(Stream input, Stream output, CancellationToken? cancellationToken = null, StreamingOptions? options = default)
        {
            return OpenStreamingTransformerAsync(options, async x => await x.TransformSequenceAsync(input, output, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
        }

        public Task TransformSequenceAsync(TextReader reader, TextWriter writer, CancellationToken? cancellationToken = null, StreamingOptions? options = default)
        {
            return OpenStreamingTransformerAsync(options, async x => await x.TransformSequenceAsync(reader, writer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
        }

        public IEnumerable<ValidationIssue> Validate()
        {
            var transformer = _context.JsonTokenReader.Read(_context.JsonTransformer);

            if (transformer is null)
            {
                throw new InvalidOperationException("The transformer could not be read as valid JSON, please verify that your transformer is valid");
            }

            return new JoltTransformerValidator<TContext>(_context).Validate(transformer);
        }

        private IJsonToken? TransformToken(EvaluationToken token, IEvaluationScope scope)
        {
            var pendingNodes = new EvaluationTokenLayerReader();

            pendingNodes.Enqueue(token);

            while (pendingNodes.HasTokens)
            {
                var current = pendingNodes.GetNextToken();

                try
                {
                    // Anything in a 'using' block that isn't an expression would otherwise be passed over without complaint.

                    if (current.IsWithinStatementBlock && !IsExpressionText(current.CurrentTransformerToken))
                    {
                        throw _context.CreateExecutionErrorFor<JoltTransformer<TContext>>(ExceptionCode.UnableToUseNonStatementExpressionWithinStatementBlock, current.CurrentTransformerToken?.ToString() ?? "null");
                    }

                    // We need to check up front if the property name contains an expression and evaluate that first because
                    // some property name expressions will be responsible for generating their own values. Also, in some cases,
                    // we have already evaluated the name and need to continue to evaluate the property value portion instead of
                    // introducing cycles and evaluating the name again.

                    if (!current.IsPendingValueEvaluation &&
                        _context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrOpenCurlyBraceOrLogicalNot(current.PropertyName) &&
                        !scope.TryGetVariable(current.PropertyName, out var _))
                    {
                        var result = TransformExpression(current, current.PropertyName, EvaluationMode.PropertyName, scope);

                        ApplyChangesToParent(current.ParentToken, result, scope, current.IsWithinStatementBlock, current.IsWithinMatchBlock, current.ParentArrayIndex);

                        if (result.IsValuePendingEvaluation)
                        {
                            // Property name evaluation has already happened now, send this back around for handling the value.
                            // The parent holds its own copy of an object or array by now, and that's the one to evaluate since
                            // it's the one in the result. A variable isn't in the parent, so it keeps the token that it was given.

                            var evaluatedName = result.NewPropertyName ?? result.OriginalPropertyName;
                            var attachedToken = current.ParentToken is IJsonObject parentObject ? parentObject[evaluatedName] : default;
                            var isAttachedContainer = attachedToken is IJsonObject || attachedToken is IJsonArray;

                            var evaluationToken = new EvaluationToken(
                                evaluatedName,
                                default,
                                current.ParentToken,
                                isAttachedContainer ? attachedToken : current.CurrentTransformerToken,
                                current.CurrentSource,
                                true,
                                result.RangeVariable,
                                parentArrayIndex: current.ParentArrayIndex
                            );

                            // Evaluations further along in the document may depend on the results of evaluating
                            // this property's value, especially in the case of setting a range variable, so push
                            // it to the front of the line.

                            pendingNodes.Push(evaluationToken);
                        }
                        else if (current.IsWithinMatchBlock)
                        {
                            return result.TransformedToken;
                        }
                    }
                    else if (current.CurrentTransformerToken is IJsonObject obj)
                    {
                        var properties = obj.Select(property => new EvaluationToken(property.PropertyName, default, current.CurrentTransformerToken, property.Value, token.CurrentSource, parentRangeVariable: current.ParentRangeVariable));

                        EnqueueChildren(pendingNodes, current, properties);
                    }
                    else if (current.CurrentTransformerToken is IJsonArray array)
                    {
                        var elements = array.Select((element, index) => new EvaluationToken(current.PropertyName, default, current.CurrentTransformerToken, element, token.CurrentSource, parentRangeVariable: current.ParentRangeVariable, parentArrayIndex: index));

                        EnqueueChildren(pendingNodes, current, elements);
                    }
                    else if (current.CurrentTransformerToken is IJsonValue value && value.ValueType == JsonValueType.String)
                    {
                        var transformerPropertyValue = value.ToTypeOf<string>();

                        if (!_context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrOpenCurlyBraceOrLogicalNot(transformerPropertyValue))
                        {
                            // All transformable expressions need to be rooted in a method call, parenthesized expression, array literal,
                            // range variable, or logical NOT operator otherwise we may transform things that the user intended to be literal values.

                            continue;
                        }

                        var result = TransformExpression(current, value.ToTypeOf<string>(), EvaluationMode.PropertyValue, scope);

                        ApplyChangesToParent(current.ParentToken, result, scope, current.IsWithinStatementBlock, current.IsWithinMatchBlock, current.ParentArrayIndex);

                        // An expression given on its own (e.g. a loop's template) has no parent holding its result, so it's what gets returned.

                        if (ReferenceEquals(current, token))
                        {
                            return result.TransformedToken;
                        }
                    }
                }
                catch (Exception ex) when (_context.ErrorHandler.IsEnabled)
                {
                    // When the error handler is enabled that means we're not in strict mode anymore and catching
                    // this exception indicates that something happened on either the property name or its value
                    // that prevented it from completing. If possible, we'll fall back to the default value or
                    // just remove the node entirely so it's not cluttering the transformed JSON.

                    if (current?.ParentToken is IJsonObject obj)
                    {
                        if (current.IsPendingValueEvaluation)
                        {
                            obj[current.PropertyName] = default;
                        }
                        else
                        {
                            obj.Remove(current.PropertyName);
                        }
                    }
                    else if (current?.ParentToken is IJsonArray array && current.ParentArrayIndex is int index)
                    {
                        // Removing the element would move the ones that are still waiting to be evaluated, so it's left as null.

                        array[index] = default;
                    }

                    _context.ErrorHandler.HandleFor<JoltTransformer<TContext>>(ex);

                    if (ReferenceEquals(current, token) && current.CurrentTransformerToken is IJsonValue)
                    {
                        return default;
                    }
                }
            }

            return token.CurrentTransformerToken;
        }

        private static void EnqueueChildren(EvaluationTokenLayerReader pendingNodes, EvaluationToken parent, IEnumerable<EvaluationToken> children)
        {
            // A value that was deferred until its property name was evaluated may be a variable that later
            // evaluations depend on, so its children need to finish first (see where it's pushed above).

            if (parent.IsPendingValueEvaluation)
            {
                pendingNodes.Push(children.ToArray());

                return;
            }

            foreach (var child in children)
            {
                pendingNodes.Enqueue(child);
            }
        }

        private bool IsExpressionText(IJsonToken token)
        {
            return token is IJsonValue value
                && value.ValueType == JsonValueType.String
                && _context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrOpenCurlyBraceOrLogicalNot(value.ToTypeOf<string>());
        }

        private EvaluationResult? TransformExpression(EvaluationToken token, string expressionText, EvaluationMode evaluationMode, IEvaluationScope scope)
        {
            _context.WriteDebugFor<JoltTransformer<TContext>>($"Transforming expression '{expressionText}' in '{evaluationMode}' mode.");
            _context.WriteInfoFor<JoltTransformer<TContext>>($"Transforming expression '{expressionText}'.");

            var actualTokens = _context.TokenReader.ReadToEnd(expressionText, evaluationMode);

            if (!_context.ExpressionParser.TryParseExpression(actualTokens, _context, out var expression))
            {
                if (token.IsWithinStatementBlock)
                {
                    throw _context.CreateExecutionErrorFor<JoltTransformer<TContext>>(ExceptionCode.UnableToUseNonStatementExpressionWithinStatementBlock, expressionText);
                }

                _context.WriteInfoFor<JoltTransformer<TContext>>($"Expression '{expressionText}' could not be parsed, skipping transformation.");

                return new EvaluationResult(token.PropertyName, null, token.CurrentTransformerToken);
            }

            // Checking the whole statement up front means that where a statement is used is verified whether or
            // not it would have been reached, and before anything that it changes has been changed.

            if (token.StatementVariableName != null)
            {
                foreach (var violation in StatementRules.GetViolationsFor(expression!, token.StatementVariableName, x => scope.ContainsVariable(x)))
                {
                    throw _context.CreateExecutionErrorFor<JoltTransformer<TContext>>(violation.Code, violation.Parameters);
                }
            }

            var evaluationContext = new EvaluationContext(
                evaluationMode, 
                expression, 
                _context,
                token,
                scope,
                TransformToken);

            return _context.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        private void ApplyChangesToParent(IJsonToken parent, EvaluationResult result, IEvaluationScope scope, bool isWithinStatementBlock, bool isWithinMatchBlock, int? parentArrayIndex)
        {            
            void SetVariableIfPresent(IJsonObject? json, string propertyName)
            {
                if (result.RangeVariable != null && !scope.TryGetVariable(result.RangeVariable.Name, out var _))
                {
                    _context.WriteDebugFor<JoltTransformer<TContext>>($"Setting new variable '{result.RangeVariable.Name}' in scope with transformed token.");

                    if (result.RangeVariable.Name == propertyName)
                    {
                        json?.Remove(propertyName);
                    }
                    
                    result.RangeVariable.Value = result.TransformedToken;

                    scope.AddOrUpdateVariable(result.RangeVariable, forceApplyToCurrentLayer: true);                    

                    return;
                }

                if (scope.TryGetVariable(propertyName, out var variable))
                {
                    _context.WriteDebugFor<JoltTransformer<TContext>>($"Updating variable '{variable.Name}' in scope with transformed token.");

                    json?.Remove(propertyName);

                    var updatedVariable = new RangeVariable(variable.Name, result.TransformedToken);

                    scope.AddOrUpdateVariable(updatedVariable, forceApplyToCurrentLayer: true);
                }
            }

            if (parent is null)
            {
                _context.WriteDebugFor<JoltTransformer<TContext>>("No parent token available to apply changes to, skipping.");

                return;
            }

            if (parent is IJsonObject obj)
            {
                _context.WriteInfoFor<JoltTransformer<TContext>>($"Applying transformed property '{result.OriginalPropertyName}' to parent object.");

                if (string.IsNullOrWhiteSpace(result.NewPropertyName))
                {
                    _context.WriteDebugFor<JoltTransformer<TContext>>($"No new property name specified, using original property name '{result.OriginalPropertyName}'.");

                    obj[result.OriginalPropertyName] = result.TransformedToken;

                    SetVariableIfPresent(obj, result.OriginalPropertyName);
                }
                else
                { 
                    _context.WriteDebugFor<JoltTransformer<TContext>>($"Applying transformed property with new name '{result.NewPropertyName}', replacing old name '{result.OriginalPropertyName}'.");

                    obj.Remove(result.OriginalPropertyName);
                    obj[result.NewPropertyName] = result.TransformedToken;

                    SetVariableIfPresent(obj, result.NewPropertyName);
                }
            }
            else if (parent is IJsonArray array)
            {
                // An element of the transformer's own array is replaced by its result, wherever it is in the array.

                if (parentArrayIndex is int index)
                {
                    _context.WriteInfoFor<JoltTransformer<TContext>>($"Replacing entry at index '{index}' of parent array.");

                    array[index] = result.TransformedToken;
                }

                // Within a statement or match block, all transformation work will be done on a variable and
                // won't need to be merged until it all collects at the end, so we're skipping that case.

                else if (result.TransformedToken != null && !isWithinStatementBlock && !isWithinMatchBlock)
                {
                    _context.WriteInfoFor<JoltTransformer<TContext>>($"Adding entry to non-statement parent array.");

                    array.Add(result.TransformedToken);                 
                }
            }
            else
            {
                throw _context.CreateExecutionErrorFor<JoltTransformer<TContext>>(ExceptionCode.UnableToApplyChangesToUnsupportedParentToken, parent.Type);
            }
        }

        protected static IEnumerable<MethodRegistration> GetExternalMethodRegistrationsFrom<T>()
        {
            var type = typeof(T);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            return from method in methods
                   let attribute = method.GetCustomAttribute<JoltExternalMethodAttribute>()
                   where attribute != null
                   select method.IsStatic ? MethodRegistration.FromStaticMethod(type, method.Name, attribute.Name) : MethodRegistration.FromInstanceMethod(method.Name, attribute.Name);
        }

        private void OpenStreamingTransformer(StreamingOptions? streamingOptions, Action<JoltStreamingTransformer<TContext>> useTransformer)
        {
            var transformer = new JoltStreamingTransformer<TContext>(this, streamingOptions);
            useTransformer(transformer);
        }

        private Task OpenStreamingTransformerAsync(StreamingOptions? streamingOptions, Func<JoltStreamingTransformer<TContext>, Task> useTransformer)
        {
            var transformer = new JoltStreamingTransformer<TContext>(this, streamingOptions);
            return useTransformer(transformer);
        }
    }
}
