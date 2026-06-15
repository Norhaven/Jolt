using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
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

        public JoltTransformer(TContext context)
        {
            _context = context;

            _context.ReferenceResolver.Clear();
            _context.ReferenceResolver.RegisterMethods(_context.MethodRegistrations, _context.MethodContext);
        }

        public string? Transform(string json)
        {
            var source = _context.JsonTokenReader.Read(json);

            if (source == null)
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

        public void Transform(Stream input, Stream output)
        {
            using var reader = new StreamReader(input);
            using var writer = new StreamWriter(output, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (line is null)
                {
                    continue;
                }

                var transformed = Transform(line);

                if (transformed is null)
                {
                    continue;
                }

                writer.WriteLine(transformed);
            }
        }

        public void Transform(TextReader input, TextWriter output)
        {
            while (input.Peek() != -1)
            {
                var line = input.ReadLine();

                if (line is null)
                {
                    continue;
                }

                var transformed = Transform(line);

                if (transformed is null)
                {
                    continue;
                }

                output.WriteLine(transformed);
            }
        }

        public async Task TransformAsync(Stream input, Stream output, CancellationToken? cancellationToken = default)
        {
            cancellationToken = cancellationToken ?? CancellationToken.None;

            using var reader = new StreamReader(input);
            using var writer = new StreamWriter(output, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            var currentLine = await reader.ReadLineAsync();

            while(currentLine != null)
            {
                var transformed = Transform(currentLine);

                if (transformed is null)
                {
                    continue;
                }

                await writer.WriteLineAsync(transformed);
                
                if (cancellationToken.Value.IsCancellationRequested)
                {
                    break;
                }

                currentLine = await reader.ReadLineAsync();
            }
        }

        public async Task TransformAsync(TextReader input, TextWriter output, CancellationToken? cancellationToken = default)
        {
            cancellationToken = cancellationToken ?? CancellationToken.None;

            var currentLine = await input.ReadLineAsync();

            while (currentLine != null)
            {
                var transformed = Transform(currentLine);

                if (transformed is null)
                {
                    continue;
                }

                await output.WriteLineAsync(transformed);

                if (cancellationToken.Value.IsCancellationRequested)
                {
                    break;
                }

                currentLine = await input.ReadLineAsync();
            }
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
                    // We need to check up front if the property name contains an expression and evaluate that first because
                    // some property name expressions will be responsible for generating their own values. Also, in some cases,
                    // we have already evaluated the name and need to continue to evaluate the property value portion instead of
                    // introducing cycles and evaluating the name again.

                    if (!current.IsPendingValueEvaluation &&
                        _context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrOpenCurlyBraceOrLogicalNot(current.PropertyName) &&
                        !scope.TryGetVariable(current.PropertyName, out var _))
                    {
                        var result = TransformExpression(current, current.PropertyName, EvaluationMode.PropertyName, scope);

                        ApplyChangesToParent(current.ParentToken, result, scope, current.IsWithinStatementBlock);

                        if (result.IsValuePendingEvaluation)
                        {
                            // Property name evaluation has already happened now, send this back around for handling the value.

                            var evaluationToken = new EvaluationToken(
                                result.NewPropertyName ?? result.OriginalPropertyName,
                                default,
                                current.ParentToken,
                                current.CurrentTransformerToken,
                                current.CurrentSource,
                                true,
                                result.RangeVariable
                            );

                            // Evaluations further along in the document may depend on the results of evaluating
                            // this property's value, especially in the case of setting a range variable, so push
                            // it to the front of the line.

                            pendingNodes.Push(evaluationToken);
                        }
                    }
                    else if (current.CurrentTransformerToken is IJsonObject obj)
                    {
                        foreach (var property in obj)
                        {
                            pendingNodes.Enqueue(new EvaluationToken(property.PropertyName, default, current.CurrentTransformerToken, property.Value, token.CurrentSource, parentRangeVariable: current.ParentRangeVariable));
                        }
                    }
                    else if (current.CurrentTransformerToken is IJsonArray array)
                    {
                        foreach (var element in array)
                        {
                            pendingNodes.Enqueue(new EvaluationToken(current.PropertyName, default, current.CurrentTransformerToken, element, token.CurrentSource, parentRangeVariable: current.ParentRangeVariable));
                        }
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

                        ApplyChangesToParent(current.ParentToken, result, scope, current.IsWithinStatementBlock);
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

                    _context.ErrorHandler.HandleFor<JoltTransformer<TContext>>(ex);
                }
            }

            return token.CurrentTransformerToken;
        }

        private EvaluationResult? TransformExpression(EvaluationToken token, string expressionText, EvaluationMode evaluationMode, IEvaluationScope scope)
        {
            _context.WriteDebugFor<JoltTransformer<TContext>>($"Transforming expression '{expressionText}' in '{evaluationMode}' mode.");
            _context.WriteInfoFor<JoltTransformer<TContext>>($"Transforming expression '{expressionText}'.");

            var actualTokens = _context.TokenReader.ReadToEnd(expressionText, evaluationMode);

            if (!_context.ExpressionParser.TryParseExpression(actualTokens, _context, out var expression))
            {
                _context.WriteInfoFor<JoltTransformer<TContext>>($"Expression '{expressionText}' could not be parsed, skipping transformation.");

                return new EvaluationResult(token.PropertyName, null, token.CurrentTransformerToken);
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

        private void ApplyChangesToParent(IJsonToken parent, EvaluationResult result, IEvaluationScope scope, bool isWithinStatementBlock)
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
                // Within a statement block, all transformation work will be done on a variable and
                // won't need to be merged until it all collects at the end, so we're skipping that case.

                if (result.TransformedToken != null && !isWithinStatementBlock)
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
    }
}
