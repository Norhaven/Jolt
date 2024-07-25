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

                if (_context.TokenReader.StartsWithMethodCall(current.PropertyName))
                {
                    var result = TransformExpression(current, current.PropertyName, EvaluationMode.PropertyName, closureSources);

                    ApplyChangesToParent(current.ParentToken, result);
                }
                else if (current.CurrentTransformerToken is IJsonObject obj)
                {
                    foreach(var property in obj)
                    {
                        pendingNodes.Enqueue(new EvaluationToken(property.Key, default, current.CurrentTransformerToken, property.Value, token.Index));
                    }
                }
                else if (current.CurrentTransformerToken is IJsonArray array)
                {
                    foreach(var element in array)
                    {
                        pendingNodes.Enqueue(new EvaluationToken(current.PropertyName, default, current.CurrentTransformerToken, element, token.Index));
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
    }
}
