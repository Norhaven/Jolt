using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Jolt.Library
{
    internal static class StandardLibraryMethods
    {
        [JoltLibraryMethod("valueOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ValueOf(string path, EvaluationContext context)
        {
            // ValueOf will always start a search from the root, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromRoot);
        }

        [JoltLibraryMethod("exists")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
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
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static EvaluationResult? If(object? result, [LazyEvaluation]Expression trueExpression, [LazyEvaluation]Expression falseExpression, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(result);

            var isTrue = resolved switch
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

        [JoltLibraryMethod("includeIf", true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? IncludeIf(object? result, EvaluationContext context)
        {
            var resolvedToken = context.ResolveQueryPathIfPresent(result);

            var shouldInclude = resolvedToken switch
            {
                bool include => include,
                IJsonValue value when value.IsBoolean() => value.ToTypeOf<bool>(),
                _ => throw new JoltExecutionException($"Unable to complete #includeIf() call, failed to identify result '{result}' as a boolean value")
            };

            if (!shouldInclude)
            {
                return default;
            }

            var includedJson = context.Token.CurrentTransformerToken;
            var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;
            var currentSource = new SourceToken(0, default);

            var evaluationToken = new EvaluationToken(propertyName, default, includedJson.Parent, includedJson, currentSource);

            return context.Transform(evaluationToken, context.ClosureSources);
        }

        [JoltLibraryMethod("eval")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
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
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IEnumerable<IJsonToken> LoopOnArrayOrObjectAtPath(string path, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object))
            {
                throw new JoltExecutionException($"Unable to loop using non-enumerable transformer token of type '{token.Type}'");
            }

            var closestViableSourceToken = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);

            if (closestViableSourceToken?.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object) != true)
            {
                throw new JoltExecutionException($"Unable to loop over non-enumerable source token of type '{closestViableSourceToken?.Type}'");
            }

            var contentTemplate = token.Type switch
            { 
                JsonTokenType.Array => token.AsArray().RemoveAt(0),
                JsonTokenType.Object => token.AsObject().Copy(),
                _ => throw new JoltExecutionException($"Unable to locate loop content template for unsupported token type '{token.Type}'")
            };

            token.Clear();

            IEnumerable<IJsonToken> EnumerateClosestSourceToken()
            {
                var index = 0;

                (EvaluationToken, IJsonToken) CreateEvaluationToken(IJsonToken currentToken)
                {
                    var property = currentToken as IJsonProperty;
                    var templateCopy = contentTemplate.Copy();
                    var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;
                    var currentSource = new SourceToken(index, property);

                    var evaluationToken = new EvaluationToken(propertyName, default, token, templateCopy, currentSource);

                    return (evaluationToken, property?.Value ?? currentToken);
                }

                IJsonToken TransformEvaluationToken(IJsonToken elementOrProperty)
                {
                    (var templateEvaluationToken, var closureSource) = CreateEvaluationToken(elementOrProperty);

                    context.ClosureSources.Push(closureSource);

                    var transformedToken = context.Transform(templateEvaluationToken, context.ClosureSources);

                    context.ClosureSources.Pop();

                    index++;

                    return transformedToken;
                }

                if (closestViableSourceToken is IJsonArray array)
                {
                    foreach(var element in array)
                    {
                        yield return TransformEvaluationToken(element);
                    }
                }
                else if (closestViableSourceToken is IJsonObject obj)
                {
                    foreach(var property in obj)
                    {
                        yield return TransformEvaluationToken(property);
                    }
                }
                else
                {
                    throw new JoltExecutionException($"Unable to loop over non-enumerable token type '{closestViableSourceToken.Type}'");
                }
            }

            return EnumerateClosestSourceToken();
        }

        [JoltLibraryMethod("loopValueOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken LoopValueOf(string path, EvaluationContext context)
        {
            // LoopValueOf will always search from the closest node, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);
        }

        [JoltLibraryMethod("loopValue")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? LoopValue(EvaluationContext context)
        {
            return context.ClosureSources.Peek();
        }

        [JoltLibraryMethod("loopIndex")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? LoopIndex(EvaluationContext context)
        {
            return context.CreateTokenFrom(context.Token.CurrentSource.Index);
        }

        [JoltLibraryMethod("loopProperty")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? LoopProperty(EvaluationContext context)
        {
            return context.Token.CurrentSource.Property;
        }

        [JoltLibraryMethod("indexOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IndexOf(object? value, string searchText, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var index = resolved switch
            {
                string text => text.IndexOf(searchText),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().IndexOf(searchText),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get index of '{searchText}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(index);
        }

        [JoltLibraryMethod("length")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Length(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var length = resolved switch
            {
                string text => text.Length,
                IJsonArray array => array.Count(),
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => token.AsValue().AsObject<string>().Length,
                object[] array => array.Length,
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get length for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(length);
        }

        [JoltLibraryMethod("substring")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Substring(object? value, Range range, EvaluationContext context)
        {   
            string AsString(IJsonValue token) => token.AsValue().AsObject<string>();
            
            var resolved = context.ResolveQueryPathIfPresent(value);

            var content = resolved switch
            {
                string text => text.Substring(range),
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => AsString(token).Substring(range),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get substring for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(content);
        }

        [JoltLibraryMethod("groupBy")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? GroupBy(object? value, string propertyName, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var grouping = resolved switch
            {
                IJsonArray array => array.GroupBy(x => ((IJsonObject)x)[propertyName]?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to group by '{propertyName}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("orderBy")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderBy(object? value, string propertyName, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var grouping = resolved switch
            {
                IJsonArray array => array.OrderBy(x => ((IJsonObject)x)[propertyName]?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to order by '{propertyName}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("orderByDesc")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderByDescending(object? value, string propertyName, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var grouping = resolved switch
            {
                IJsonArray array => array.OrderByDescending(x => ((IJsonObject)x)[propertyName]?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to group by '{propertyName}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("contains")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Contains(object? instance, object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var contains = resolved switch
            {
                string text when value is string valueText => text.Contains(valueText),
                IJsonArray array => array.Contains(context.CreateTokenFrom(value)),
                IJsonValue token when token.ValueType == JsonValueType.String => token.AsValue().AsObject<string>().Contains(value?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine contents for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(contains);
        }

        [JoltLibraryMethod("roundTo")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? RoundTo(object? value, object? decimalPlaces, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var rounded = resolved switch
            {
                long i => i,
                double d when decimalPlaces is long i => Math.Round(d, (int)i),
                double d when decimalPlaces is IJsonValue token && token.ValueType == JsonValueType.Number => Math.Round(d, token.AsObject<int>()),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is long i => Math.Round(token.AsObject<double>(), (int)i),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is IJsonValue val && val.ValueType == JsonValueType.Number => Math.Round(token.AsObject<double>(), val.AsObject<int>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine rounding for unsupported object types '{value?.GetType()}' and '{decimalPlaces?.GetType()}'")
            };

            return context.CreateTokenFrom(rounded);
        }

        [JoltLibraryMethod("max")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Maximum(object? value, EvaluationContext context)
        {
            return AsIntegerOrFloatingPoint(value, x => x.Max(), x => x.Max(), context);
        }

        [JoltLibraryMethod("min")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Minimum(object? value, EvaluationContext context)
        {
            return AsIntegerOrFloatingPoint(value, x => x.Min(), x => x.Min(), context);
        }

        [JoltLibraryMethod("sum")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Sum(object? value, EvaluationContext context)
        {
            return AsIntegerOrFloatingPoint(value, x => x.Sum(), x => x.Sum(), context);
        }

        [JoltLibraryMethod("average")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Average(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var average = resolved switch
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
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? JoinWith(object? value, string delimiter, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);
            
            var joined = resolved switch
            {
                IEnumerable<string> strings => string.Join(delimiter, strings),
                IJsonArray array when array.AllElementsAreOfType<string>() => string.Join(delimiter, array.Select(x => x.AsValue().AsObject<string>())),
                IJsonArray array => string.Join(delimiter, array.Select(x => x.AsValue().AsObject<object>()?.ToString())),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to join with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(joined);
        }

        [JoltLibraryMethod("splitOn")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? SplitOn(object? value, string delimiter, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var split = resolved switch
            {
                string text => text.Split(delimiter),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().Split(delimiter),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to split with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(split);
        }

        [JoltLibraryMethod("append")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Append(object? value, [VariadicEvaluation] object[]? additionalValues, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            IJsonToken? resultToken = context.CreateTokenFrom(resolved); 

            foreach (var additionalValue in additionalValues)
            {
                var resolvedValue = context.ResolveQueryPathIfPresent(additionalValue);

                resultToken = (resultToken, resolvedValue) switch
                {
                    (IJsonArray first, IJsonArray second) => context.CreateArrayFrom(first.Concat(second).ToArray()),
                    (IJsonObject first, IJsonObject second) => context.CreateObjectFrom(first.Concat(second).ToArray()),
                    (IEnumerable<object> first, IEnumerable<object> second) => context.CreateTokenFrom(first.Concat(second).ToArray()),
                    (IJsonValue first, string second) when first.IsString() => context.CreateTokenFrom($"{first.ToTypeOf<string>()}{second}"),
                    (IJsonValue first, IJsonValue second) when first.IsString() && second.IsString() => context.CreateTokenFrom($"{first}{second}"),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to append with unsupported object types '{value?.GetType()}' and '{resolvedValue?.GetType()}'")
                };
            }

            return resultToken;
        }

        [JoltLibraryMethod("isInteger")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsInteger(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return context.CreateTokenFrom(resolved is int || resolved is long);
        }

        [JoltLibraryMethod("isString")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsString(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return context.CreateTokenFrom(resolved is string);
        }

        [JoltLibraryMethod("isDecimal")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsDecimal(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return context.CreateTokenFrom(resolved is double);
        }

        [JoltLibraryMethod("isBoolean")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsBoolean(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return context.CreateTokenFrom(resolved is bool);
        }

        [JoltLibraryMethod("isArray")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsArray(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return context.CreateTokenFrom(resolved?.GetType().IsArray == true || resolved is IJsonArray);
        }

        [JoltLibraryMethod("isEmpty")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsEmpty(object? value, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }

            var resolved = context.ResolveQueryPathIfPresent(value);

            var empty = resolved switch
            {
                IJsonArray array => array.Length == 0,
                IJsonValue val when val.IsString() => val.ToTypeOf<string>().Length == 0,
                string val => val.Length == 0,
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check emptiness with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("any")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Any(object? value, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }
            
            var resolved = context.ResolveQueryPathIfPresent(value);

            var empty = resolved switch
            {
                IJsonArray array => array.Length > 0,
                IJsonValue val when val.IsString() => val.ToTypeOf<string>().Length > 0,
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("toInteger")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToInteger(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return ConvertToType<long>(resolved, context);
        }

        [JoltLibraryMethod("toString")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToString(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var convertedValue = resolved?.ToString();
            
            return context.CreateTokenFrom(convertedValue);
        }

        [JoltLibraryMethod("toDecimal")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToDecimal(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return ConvertToType<double>(resolved, context);
        }

        [JoltLibraryMethod("toBoolean")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToBool(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            return ConvertToType<bool>(resolved, context);
        }

        private static IJsonToken? AsIntegerOrFloatingPoint(object? value, Func<IEnumerable<long>, long?> asInt64, Func<IEnumerable<double>, double> asDouble, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            // We're separating out the integer sum from the floating point so we won't
            // potentially get a rounding representation error by always defaulting to double.

            var integerValue = resolved switch
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
