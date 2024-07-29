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
        private readonly ConcurrentDictionary<string, Expression> _cachedExpressions = new ConcurrentDictionary<string, Expression>();
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

            return TransformToken(transformation, new Stack<IJsonToken>(new[] { source }))?.ToString();
        }

        private IJsonToken? TransformToken(EvaluationToken token, Stack<IJsonToken> closureSources)
        {
            var pendingNodes = new Queue<EvaluationToken>();

            pendingNodes.Enqueue(token);

            while (pendingNodes.Count > 0)
            {
                var current = pendingNodes.Dequeue();

                if (!current.IsPendingValueEvaluation && _context.TokenReader.StartsWithMethodCall(current.PropertyName))
                {
                    var result = TransformExpression(current, current.PropertyName, EvaluationMode.PropertyName, closureSources);

                    ApplyChangesToParent(current.ParentToken, result);

                    if (result.IsValuePendingEvaluation)
                    {
                        var evaluationToken = new EvaluationToken(
                            result.NewPropertyName ?? result.OriginalPropertyName,
                            default,
                            current.ParentToken,
                            current.CurrentTransformerToken,
                            current.CurrentSource,
                            true
                        );

                        pendingNodes.Enqueue(evaluationToken);
                    }
                }
                else if (current.CurrentTransformerToken is IJsonObject obj)
                {
                    foreach(var property in obj)
                    {
                        pendingNodes.Enqueue(new EvaluationToken(property.PropertyName, default, current.CurrentTransformerToken, property.Value, token.CurrentSource));
                    }
                }
                else if (current.CurrentTransformerToken is IJsonArray array)
                {
                    foreach(var element in array)
                    {
                        pendingNodes.Enqueue(new EvaluationToken(current.PropertyName, default, current.CurrentTransformerToken, element, token.CurrentSource));
                    }
                }
                else if (current.CurrentTransformerToken is IJsonValue value && value.ValueType == JsonValueType.String)
                {
                    var transformerPropertyValue = value.AsObject<string>();

                    if (!_context.TokenReader.StartsWithMethodCall(transformerPropertyValue))
                    {
                        // All transformable expressions need to be rooted in a method call otherwise
                        // we may transform things that the user intended to be literal values.

                        continue;
                    }

                    var result = TransformExpression(current, value.AsObject<string>(), EvaluationMode.PropertyValue, closureSources);

                    ApplyChangesToParent(current.ParentToken, result);
                }
            }

            return token.CurrentTransformerToken;
        }

        private EvaluationResult? TransformExpression(EvaluationToken token, string expressionText, EvaluationMode evaluationMode, Stack<IJsonToken> closureSources)
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
                TransformToken);

            return _context.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        private static void ApplyChangesToParent(IJsonToken parent, EvaluationResult result)
        {
            if (parent is null)
            {
                return;
            }

            if (parent is IJsonObject obj)
            {
                if (string.IsNullOrWhiteSpace(result.NewPropertyName))
                {
                    obj[result.OriginalPropertyName] = result.TransformedToken;
                }
                else
                { 
                    obj.Remove(result.OriginalPropertyName);
                    obj[result.NewPropertyName] = result.TransformedToken;
                }
            }
            else if (parent is IJsonArray array)
            {
                array.Add(result.TransformedToken);
            }
            else
            {
                throw new JoltExecutionException($"Unable to apply changes to parent token with unsupported type '{parent.Type}'");
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
