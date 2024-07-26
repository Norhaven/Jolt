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

            if (pathOrValue is string path && context.JsonContext.QueryPathProvider.IsQueryPath(path))
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
        public static EvaluationResult? Evaluate(string pathOrLiteral, EvaluationContext context)
        {
            var actualTokens = context.JsonContext.TokenReader.ReadToEnd(pathOrLiteral, context.Mode);

            if (!context.JsonContext.ExpressionParser.TryParseExpression(actualTokens, context.JsonContext.ReferenceResolver, out var expression))
            {
                throw new JoltExecutionException($"Unable to parse expression within #eval call with path or literal '{pathOrLiteral}'");
            }

            var evaluationContext = new EvaluationContext(
                context.Mode,
                expression,
                context.JsonContext,
                context.Token,
                context.ClosureSources,
                context.Transform);

            return context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        [JoltLibraryMethod("loop", true)]
        public static IEnumerable<IJsonToken> LoopOnArrayOrObjectAtPath(string path, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object))
            {
                yield break;
            }

            var closestViableNode = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);

            if (closestViableNode?.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object) != true)
            {
                yield break;
            }

            var contentTemplate = token.Type switch
            { 
                JsonTokenType.Array => token.AsArray().RemoveAt(0),
                JsonTokenType.Object => token.AsObject().Copy(),
                _ => throw new JoltExecutionException($"Unable to locate loop content template for unsupported token type '{token.Type}'")
            };

            token.Clear();

            var index = 0;

            if (token.Type == JsonTokenType.Array)
            {
                foreach (var element in closestViableNode.AsArray() ?? Enumerable.Empty<IJsonToken>())
                {
                    var templateCopy = contentTemplate.Copy();
                    var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;
                    var currentSource = new SourceToken(index, default);

                    var templateEvaluationToken = new EvaluationToken(propertyName, default, token, templateCopy, currentSource);

                    context.ClosureSources.Push(element);

                    yield return context.Transform(templateEvaluationToken, context.ClosureSources);

                    context.ClosureSources.Pop();

                    index++;
                }
            }
            else if (token.Type == JsonTokenType.Object)
            {
                foreach (var property in closestViableNode.AsObject() ?? Enumerable.Empty<IJsonProperty>())
                {
                    var templateCopy = contentTemplate.Copy();
                    var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;
                    var currentSource = new SourceToken(index, property);

                    var templateEvaluationToken = new EvaluationToken(propertyName, default, token, templateCopy, currentSource);

                    context.ClosureSources.Push(property.Value);

                    yield return context.Transform(templateEvaluationToken, context.ClosureSources);

                    context.ClosureSources.Pop();

                    index++;
                }
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

        [JoltLibraryMethod("loopIndex")]
        public static IJsonToken? LoopIndex(EvaluationContext context)
        {
            return context.CreateTokenFrom(context.Token.CurrentSource.Index);
        }

        [JoltLibraryMethod("loopProperty")]
        public static IJsonToken? LoopProperty(EvaluationContext context)
        {
            return context.Token.CurrentSource.Property;
        }

        [JoltLibraryMethod("indexOf")]
        public static IJsonToken? IndexOf(object? value, string searchText, EvaluationContext context)
        {
            var index = value switch
            {
                string text => text.IndexOf(searchText),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().IndexOf(searchText),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get index of '{searchText}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(index);
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

        [JoltLibraryMethod("max")]
        public static IJsonToken? Maximum(object? value, EvaluationContext context)
        {
            return AsIntegerOrFloatingPoint(value, x => x.Max(), x => x.Max(), context);
        }

        [JoltLibraryMethod("min")]
        public static IJsonToken? Minimum(object? value, EvaluationContext context)
        {
            return AsIntegerOrFloatingPoint(value, x => x.Min(), x => x.Min(), context);
        }

        [JoltLibraryMethod("sum")]
        public static IJsonToken? Sum(object? value, EvaluationContext context)
        {
            return AsIntegerOrFloatingPoint(value, x => x.Sum(), x => x.Sum(), context);
        }

        [JoltLibraryMethod("average")]
        public static IJsonToken? Average(object? value, EvaluationContext context)
        {
            if (value is string text && context.JsonContext.QueryPathProvider.IsQueryPath(text))
            {
                value = ValueOf(text, context);
            }

            var average = value switch
            {
                IEnumerable<int> integers => integers.Average(),
                IEnumerable<double> doubles => doubles.Average(),
                IJsonArray array when array.IsOnlyIntegers() => array.AsSequenceOf<long>().Average(),
                IJsonArray array when array.IsOnlyNumericPrimitives() => array.AsSequenceOf<double>().Average(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get average for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(average);
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

        [JoltLibraryMethod("splitOn")]
        public static IJsonToken? SplitOn(object? value, string delimiter, EvaluationContext context)
        {
            var split = value switch
            {
                string text => text.Split(delimiter),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().Split(delimiter),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to split with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(split);
        }

        [JoltLibraryMethod("append")]
        public static IJsonToken? Append(object? value, object? additionalValue, EvaluationContext context)
        {
            return (value, additionalValue) switch
            {
                (IJsonArray first, IJsonArray second) => context.JsonContext.JsonTokenReader.CreateArrayFrom(first.Concat(second).ToArray()),
                (IJsonObject first, IJsonObject second) => context.JsonContext.JsonTokenReader.CreateObjectFrom(first.Concat(second).ToArray()),
                (IEnumerable<object> first, IEnumerable<object> second) => context.CreateTokenFrom(first.Concat(second).ToArray()),
                (string first, string second) => context.CreateTokenFrom($"{first}{second}"),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to append with unsupported object types '{value?.GetType()}' and '{additionalValue?.GetType()}'")
            };
        }

        [JoltLibraryMethod("isInteger")]
        public static IJsonToken? IsInteger(object? value, EvaluationContext context)
        {
            return context.CreateTokenFrom(value is int || value is long);
        }

        [JoltLibraryMethod("isString")]
        public static IJsonToken? IsString(object? value, EvaluationContext context)
        {
            return context.CreateTokenFrom(value is string);
        }

        [JoltLibraryMethod("isDecimal")]
        public static IJsonToken? IsDecimal(object? value, EvaluationContext context)
        {
            return context.CreateTokenFrom(value is double);
        }

        [JoltLibraryMethod("isBoolean")]
        public static IJsonToken? IsBoolean(object? value, EvaluationContext context)
        {
            return context.CreateTokenFrom(value is bool);
        }

        [JoltLibraryMethod("isArray")]
        public static IJsonToken? IsArray(object? value, EvaluationContext context)
        {
            return context.CreateTokenFrom(value?.GetType().IsArray == true || value is IJsonArray);
        }

        [JoltLibraryMethod("toInteger")]
        public static IJsonToken? ToInteger(object? value, EvaluationContext context)
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

        [JoltLibraryMethod("toBoolean")]
        public static IJsonToken? ToBool(object? value, EvaluationContext context)
        {
            return ConvertToType<bool>(value, context);
        }

        private static IJsonToken? AsIntegerOrFloatingPoint(object? value, Func<IEnumerable<long>, long?> asInt64, Func<IEnumerable<double>, double> asDouble, EvaluationContext context)
        {
            if (value is string text && context.JsonContext.QueryPathProvider.IsQueryPath(text))
            {
                value = ValueOf(text, context);
            }

            // We're separating out the integer sum from the floating point so we won't
            // potentially get a rounding representation error by always defaulting to double.

            var integerValue = value switch
            {
                IEnumerable<int> integers => asInt64(integers.Cast<long>()),
                IEnumerable<long> integers => asInt64(integers),
                IJsonArray array when array.IsOnlyIntegers() => asInt64(array.AsSequenceOf<long>()),
                _ => null
            };

            if (integerValue != null)
            {
                return context.CreateTokenFrom(integerValue);
            }

            var doubleValue = value switch
            {
                IEnumerable<double> doubles => asDouble(doubles),
                IJsonArray array when array.AllElementsAreOfType<double>() => asDouble(array.Select(x => x.AsValue().AsObject<double>())),
                IJsonArray array when array.AllElementsAreOfType<int, long, double>() => asDouble(array.Select(x => (double)x.AsValue().AsObject<object>())),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get integer or floating point value for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(doubleValue);
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
