using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library
{
    public static class StandardLibraryMethods
    {
        [JoltLibraryMethod("valueOf")]
        public static IJsonToken? ValueOf(string path, EvaluationContext context)
        {
            // ValueOf will always start a search from the root, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromRoot);
        }

        [JoltLibraryMethod("exists")]
        public static IJsonToken? Exists(object? pathOrValue, EvaluationContext context)
        {
            var valueExists = pathOrValue != null;

            if (pathOrValue is IJsonToken token)
            {
                var tokenExists = valueExists && token.Type != JsonTokenType.Null && token.AsValue().AsObject<object>() != null;

                return context.CreateTokenFrom(tokenExists);
            }

            if (pathOrValue is string path)
            {
                var tokenValue = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromRoot);

                var tokenExists = tokenValue != null && tokenValue.Type != JsonTokenType.Null;

                return context.CreateTokenFrom(tokenExists);
            }

            return context.CreateTokenFrom(valueExists);
        }

        [JoltLibraryMethod("if")]
        public static EvaluationResult? If(object? result, [LazyEvaluation]Expression trueExpression, [LazyEvaluation]Expression falseExpression, EvaluationContext context)
        {
            var isTrue = result switch
            {
                bool value => value,
                IJsonToken token => token.Type == JsonTokenType.Value && token.AsValue().ValueType == JsonValueType.Boolean && token.AsValue().ToTypeOf<bool>(),
                null => false
            };

            var expression = isTrue ? trueExpression : falseExpression;

            var evaluationContext = new EvaluationContext(
                context.Mode,
                expression,
                context.JsonContext,
                context.Token,
                context.ClosureSources,
                context.Transform
            );

            return context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        [JoltLibraryMethod("eval")]
        public static IJsonToken? Evaluate(object? result, EvaluationContext context)
        {
            return context.CreateTokenFrom(result);
        }

        [JoltLibraryMethod("loop")]
        public static IEnumerable<IJsonToken> LoopOnArrayAtPath(string path, EvaluationContext context)
        {
            if (context.Token.CurrentTransformerToken.Type != JsonTokenType.Array)
            {
                yield break;
            }

            var closestViableNode = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);

            if (closestViableNode?.Type != JsonTokenType.Array)
            {
                yield break;
            }

            var transformerArray = context.Token.CurrentTransformerToken.AsArray();

            var contentTemplate = transformerArray.RemoveAt(0);

            foreach (var propertyOrElement in closestViableNode.AsArray() ?? Enumerable.Empty<IJsonToken>())
            {
                var templateCopy = contentTemplate.Copy();
                var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;

                var templateEvaluationToken = new EvaluationToken(propertyName, default, transformerArray, templateCopy);

                context.ClosureSources.Push(propertyOrElement);

                yield return context.Transform(templateEvaluationToken, context.ClosureSources);
                
                context.ClosureSources.Pop();
            }
        }

        [JoltLibraryMethod("loopValueOf")]
        public static IJsonToken LoopValueOf(string path, EvaluationContext context)
        {
            // LoopValueOf will always search from the closest node, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);
        }

        [JoltLibraryMethod("loopValue")]
        public static IJsonToken? LoopValue(EvaluationContext context)
        {
            return context.ClosureSources.Peek();
        }

        [JoltLibraryMethod("length")]
        public static IJsonToken? Length(object? value, EvaluationContext context)
        {
            var length = value switch
            {
                string text => text.Length,
                IJsonArray array => array.Count(),
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => token.AsValue().AsObject<string>().Length,
                object[] array => array.Length,
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get length for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(length);
        }

        [JoltLibraryMethod("contains")]
        public static IJsonToken? Contains(object? instance, object? value, EvaluationContext context)
        {
            var contains = instance switch
            {
                string text when value is string valueText => text.Contains(valueText),
                IJsonArray array => array.Contains(context.CreateTokenFrom(value)),
                IJsonValue token when token.ValueType == JsonValueType.String => token.AsValue().AsObject<string>().Contains(value?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine contents for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(contains);
        }

        [JoltLibraryMethod("roundTo")]
        public static IJsonToken? RoundTo(object? value, object? decimalPlaces, EvaluationContext context)
        {
            var rounded = value switch
            {
                int i => i,
                double d when decimalPlaces is int i => Math.Round(d, i),
                double d when decimalPlaces is IJsonValue token && token.ValueType == JsonValueType.Number => Math.Round(d, token.AsObject<int>()),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is int i => Math.Round(token.AsObject<double>(), i),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is IJsonValue val && val.ValueType == JsonValueType.Number => Math.Round(token.AsObject<double>(), val.AsObject<int>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine rounding for unsupported object types '{value?.GetType()}' and '{decimalPlaces?.GetType()}'")
            };

            return context.CreateTokenFrom(rounded);
        }

        [JoltLibraryMethod("sum")]
        public static IJsonToken? Sum(object? value, EvaluationContext context)
        {
            var sum = value switch
            {
                IEnumerable<int> integers => integers.Sum(),
                IEnumerable<double> doubles => doubles.Sum(),
                IJsonArray array when array.AllElementsAreOfTypes(typeof(int), typeof(long)) => array.Sum(x => x.AsValue().AsObject<long>()),
                IJsonArray array when array.AllElementsAreOfType<double>() => array.Sum(x => x.AsValue().AsObject<double>()),
                IJsonArray array when array.AllElementsAreOfTypes(typeof(int), typeof(long), typeof(double)) => array.Sum(x => (double)x.AsValue().AsObject<object>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to sum for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(sum);
        }

        [JoltLibraryMethod("joinWith")]
        public static IJsonToken? JoinWith(object? value, string delimiter, EvaluationContext context)
        {
            var joined = value switch
            {
                IEnumerable<string> strings => string.Join(delimiter, strings),
                IJsonArray array when array.AllElementsAreOfType<string>() => string.Join(delimiter, array.Select(x => x.AsValue().AsObject<string>())),
                IJsonArray array => string.Join(delimiter, array.Select(x => x.AsValue().AsObject<object>()?.ToString())),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to join with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(joined);
        }

        [JoltLibraryMethod("toInt")]
        public static IJsonToken? ToInt(object? value, EvaluationContext context)
        {
            return ConvertToType<int>(value, context);
        }

        [JoltLibraryMethod("toString")]
        public static IJsonToken? ToString(object? value, EvaluationContext context)
        {
            var convertedValue = value?.ToString();
            
            return context.CreateTokenFrom(convertedValue);
        }

        [JoltLibraryMethod("toDecimal")]
        public static IJsonToken? ToDecimal(object? value, EvaluationContext context)
        {
            return ConvertToType<double>(value, context);
        }

        [JoltLibraryMethod("toBool")]
        public static IJsonToken? ToBool(object? value, EvaluationContext context)
        {
            return ConvertToType<bool>(value, context);
        }

        private static IJsonToken? ConvertToType<T>(object? value, EvaluationContext context)
        {
            if (value is IJsonToken token)
            {
                value = token.AsValue().ToTypeOf<object>();
            }

            if (value is null || value is T)
            {
                return context.CreateTokenFrom(value);
            }

            var convertedValue = Convert.ChangeType(value, typeof(T));

            return context.CreateTokenFrom(convertedValue);
        }
    }
}
