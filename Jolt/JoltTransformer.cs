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
using System.Linq;
using System.Reflection;
using System.Text;

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
            var transformedJson = _context.JsonTokenReader.Read(_context.JsonTransformer);
            var transformation = new EvaluationToken(default, default, default, transformedJson);

            return TransformToken(transformation, new Stack<IJsonToken>(new[] { source }), new Stack<IList<RangeVariable>>())?.ToString();
        }

        private IJsonToken? TransformToken(EvaluationToken token, Stack<IJsonToken> closureSources, Stack<IList<RangeVariable>> rangeVariables)
        {
            var pendingNodes = new EvaluationTokenLayerReader();

            pendingNodes.Enqueue(token);

            while (pendingNodes.HasTokens)
            {
                var current = pendingNodes.GetNextToken();

                // We need to check up front if the property name contains an expression and evaluate that first because
                // some property name expressions will be responsible for generating their own values. Also, in some cases,
                // we have already evaluated the name and need to continue to evaluate the property value portion instead of
                // introducing cycles and evaluating the name again.

                if (!current.IsPendingValueEvaluation && 
                    _context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariable(current.PropertyName) &&
                    (rangeVariables.Count == 0 || !rangeVariables.Peek().Any(x => x.Name == current.PropertyName)))
                {
                    var result = TransformExpression(current, current.PropertyName, EvaluationMode.PropertyName, closureSources, rangeVariables);

                    ApplyChangesToParent(current.ParentToken, result, rangeVariables);
                    
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
                    foreach(var property in obj)
                    {
                        pendingNodes.Enqueue(new EvaluationToken(property.PropertyName, default, current.CurrentTransformerToken, property.Value, token.CurrentSource, parentRangeVariable: current.ParentRangeVariable));
                    }
                }
                else if (current.CurrentTransformerToken is IJsonArray array)
                {
                    foreach(var element in array)
                    {
                        pendingNodes.Enqueue(new EvaluationToken(current.PropertyName, default, current.CurrentTransformerToken, element, token.CurrentSource, parentRangeVariable: current.ParentRangeVariable));
                    }
                }
                else if (current.CurrentTransformerToken is IJsonValue value && value.ValueType == JsonValueType.String)
                {
                    var transformerPropertyValue = value.ToTypeOf<string>();

                    if (!_context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariable(transformerPropertyValue))
                    {
                        // All transformable expressions need to be rooted in a method call, parenthesized expression,
                        // or a range variable otherwise we may transform things that the user intended to be literal values.

                        continue;
                    }

                    var result = TransformExpression(current, value.ToTypeOf<string>(), EvaluationMode.PropertyValue, closureSources, rangeVariables);

                    ApplyChangesToParent(current.ParentToken, result, rangeVariables);
                }
            }

            return token.CurrentTransformerToken;
        }

        private EvaluationResult? TransformExpression(EvaluationToken token, string expressionText, EvaluationMode evaluationMode, Stack<IJsonToken> closureSources, Stack<IList<RangeVariable>> rangeVariables)
        {
            var actualTokens = _context.TokenReader.ReadToEnd(expressionText, evaluationMode);

            if (!_context.ExpressionParser.TryParseExpression(actualTokens, _context.ReferenceResolver, out var expression))
            {
                return new EvaluationResult(token.PropertyName, null, token.CurrentTransformerToken);
            }   

            var evaluationContext = new EvaluationContext(
                evaluationMode, 
                expression, 
                _context,
                token,
                closureSources,
                rangeVariables,
                TransformToken);

            return _context.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        private static void ApplyChangesToParent(IJsonToken parent, EvaluationResult result, Stack<IList<RangeVariable>> rangeVariables)
        {            
            void SetVariableIfPresent(IJsonObject json, string propertyName)
            {
                if (!rangeVariables.TryPeek(out var variables))
                {
                    variables = new List<RangeVariable>();
                    rangeVariables.Push(variables);
                }

                if (result.RangeVariable != null && (variables.Count == 0 || !variables.Any(x => x.Name == result.RangeVariable.Name)))
                {
                    if (result.TransformedToken != null)
                    {
                        json.Remove(propertyName);
                        result.RangeVariable.Value = result.TransformedToken;
                    }

                    variables.Add(result.RangeVariable);                    

                    return;
                }

                for (var i = 0; i < variables.Count; i++)
                {
                    var variable = variables[i];

                    if (variable.Name == propertyName)
                    {
                        if (result.TransformedToken is null)
                        {
                            throw Error.CreateExecutionErrorFrom(ExceptionCode.ReferencedRangeVariableWithNoValue, result.RangeVariable.Name);
                        }

                        json.Remove(propertyName);
                        variables[i] = new RangeVariable(variable.Name, result.TransformedToken);      

                        return;
                    }
                }
            }

            if (parent is null)
            {
                return;
            }

            if (parent is IJsonObject obj)
            {
                if (string.IsNullOrWhiteSpace(result.NewPropertyName))
                {
                    obj[result.OriginalPropertyName] = result.TransformedToken;

                    SetVariableIfPresent(obj, result.OriginalPropertyName);
                }
                else
                { 
                    obj.Remove(result.OriginalPropertyName);
                    obj[result.NewPropertyName] = result.TransformedToken;

                    SetVariableIfPresent(obj, result.NewPropertyName);
                }
            }
            else if (parent is IJsonArray array)
            {
                if (result.TransformedToken != null)
                {
                    array.Add(result.TransformedToken);
                }
            }
            else
            {
                throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToApplyChangesToUnsupportedParentToken, parent.Type);
            }
        }

        protected static IEnumerable<MethodRegistration> GetExternalMethodRegistrationsFrom<T>()
        {
            var type = typeof(T);
            var methods = type.GetMethods(BindingFlags.Public);

            return from method in methods
                   let attribute = method.GetCustomAttribute<JoltExternalMethodAttribute>()
                   where attribute != null
                   select method.IsStatic ? new MethodRegistration(type.AssemblyQualifiedName, method.Name) : new MethodRegistration(method.Name, attribute.Name);
        }
    }
}
