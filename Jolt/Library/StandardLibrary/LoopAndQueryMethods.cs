using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal sealed class LoopAndQueryMethods
    {
        [JoltLibraryMethod("foreach", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IEnumerable<IJsonToken> LoopOnArrayOrObjectAtPath(Enumeration enumeration, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object))
            {
                throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableToken, token.Type);
            }

            var closestViableSourceToken = enumeration.Source switch
            {
                QueryPathReference path => context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path.Path, JsonQueryMode.StartFromClosestMatch),
                RangeVariable variable => variable.Value,
                DereferencedPath path when path.MissingPaths.Any() => default,
                DereferencedPath path => path.ObtainableToken,
                _ => throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallDueToInvalidParameter, enumeration.Source)
            };

            if (closestViableSourceToken?.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object) != true)
            {
                throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableSourceToken, closestViableSourceToken?.Type);
            }

            var contentTemplate = token.Type switch
            {
                JsonTokenType.Array => token.AsArray().RemoveAt(0),
                JsonTokenType.Object => token.AsObject().Copy(),
                _ => throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallDueToMissingContentTemplate, token.Type)
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

                    var evaluationToken = new EvaluationToken(propertyName, default, token, templateCopy, currentSource, true);

                    return (evaluationToken, property?.Value ?? currentToken);
                }

                IJsonToken TransformEvaluationToken(IJsonToken elementOrProperty)
                {
                    (var templateEvaluationToken, var closureSource) = CreateEvaluationToken(elementOrProperty);

                    // Looping adds a temporary scope for resolution purposes during each loop so that
                    // the loop variable can only be accessed during the loop and so that any range variables
                    // added in the loop have those same rules apply to them as well.

                    context.Scope.CreateClosureOver(closureSource);

                    var loopVariable = new RangeVariable(enumeration.Variable.Name, closureSource);

                    context.Scope.AddOrUpdateVariable(loopVariable);

                    if (enumeration.IndexVariable != null)
                    {
                        var indexVariable = new RangeVariable(enumeration.IndexVariable.Name, context.CreateTokenFrom(index));

                        // We want to force both variables to be a part of the same scope layer so we can just
                        // drop both at once when we're done and not have to worry about logic for multiple layers.

                        context.Scope.AddOrUpdateVariable(indexVariable, forceApplyToCurrentLayer: true);
                    }

                    var transformedToken = context.Transform(templateEvaluationToken, context.Scope);

                    context.Scope.RemoveCurrentClosure();
                    context.Scope.RemoveCurrentVariablesLayer();

                    index++;

                    return transformedToken;
                }

                if (closestViableSourceToken is IJsonArray array)
                {
                    foreach (var element in array)
                    {
                        yield return TransformEvaluationToken(element);
                    }
                }
                else if (closestViableSourceToken is IJsonObject obj)
                {
                    foreach (var property in obj)
                    {
                        yield return TransformEvaluationToken(property);
                    }
                }
                else
                {
                    throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableSourceToken, closestViableSourceToken.Type);
                }
            }

            return EnumerateClosestSourceToken();
        }

        [JoltLibraryMethod("groupBy")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? GroupBy(object? value, [LambdaVariables("x")] LambdaMethod keySelectorLambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var grouping = resolved switch
            {
                IJsonArray array => array.GroupBy(x => new QueryMethods(keySelectorLambda).ExecuteLambda(x, context)?.ToTypeOf<object>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform a group by using unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("orderBy")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderBy(object? value, [OptionalParameter(null)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var ordered = resolved switch
            {
                IJsonArray array => Order(array, lambda, descending: false, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform an order by using unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(ordered);
        }

        [JoltLibraryMethod("orderByDesc")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderByDescending(object? value, [OptionalParameter(null)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var ordered = resolved switch
            {
                IJsonArray array => Order(array, lambda, descending: true, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform an order by using unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(ordered);
        }

        /// <summary>
        /// Orders an array by the value that a lambda selects from each element or, without a lambda, by the elements themselves.
        /// Without a lambda, the elements must all be numbers, all strings or all booleans, along with any nulls (which come first,
        /// as they do in C#). Strings are compared ordinally, so that the order doesn't depend on the current culture.
        /// </summary>
        private static IEnumerable<IJsonToken> Order(IJsonArray array, LambdaMethod? lambda, bool descending, EvaluationContext context)
        {
            Func<IJsonToken, object?> keySelector;
            IComparer<object?> comparer = Comparer<object?>.Default;

            if (lambda != null)
            {
                keySelector = x => new QueryMethods(array, lambda).ExecuteLambda(x, context)?.ToTypeOf<object>();
            }
            else
            {
                var values = array.Where(x => !ValueResolutionExtensions.IsNull(x)).ToArray();
                var kinds = values.Select(DescribeSortableKind).Distinct().ToArray();

                if (kinds.Length > 1 || kinds.Any(x => x is "arrays" || x is "objects"))
                {
                    throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToOrderUnsortableArrayWithoutLambda, string.Join(", ", kinds));
                }

                // Integers are compared as integers unless there's a decimal among them, so that large integers keep their precision.

                var hasDecimal = values.Any(x => !(x.AsValue().ToTypeOf<object>() is long));

                keySelector = x => ValueResolutionExtensions.IsNull(x) ? null : x.AsValue().ToTypeOf<object>() switch
                {
                    long integer when hasDecimal => (double)integer,
                    var value => value
                };

                if (kinds.FirstOrDefault() == "strings")
                {
                    comparer = Comparer<object?>.Create((x, y) => string.CompareOrdinal((string?)x, (string?)y));
                }
            }

            return descending ? array.OrderByDescending(keySelector, comparer) : array.OrderBy(keySelector, comparer);
        }

        private static string DescribeSortableKind(IJsonToken token)
        {
            return token.Type switch
            {
                JsonTokenType.Array => "arrays",
                JsonTokenType.Object => "objects",
                _ => token.AsValue().ValueType switch
                {
                    JsonValueType.Number => "numbers",
                    JsonValueType.String => "strings",
                    JsonValueType.Boolean => "booleans",
                    var other => other.ToString().ToLowerInvariant()
                }
            };
        }

        [JoltLibraryMethod("max")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Maximum(object? value, [OptionalParameter(default)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = ResolveValueWithOptionalProjection(value, lambda, context);

            if (resolved is null)
            {
                return context.CreateNullToken();
            }

            return AsIntegerOrFloatingPoint(resolved, x => x.Max(), x => x.Max(), context);
        }

        [JoltLibraryMethod("min")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Minimum(object? value, [OptionalParameter(default)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = ResolveValueWithOptionalProjection(value, lambda, context);

            if (resolved is null)
            {
                return context.CreateNullToken();
            }

            return AsIntegerOrFloatingPoint(resolved, x => x.Min(), x => x.Min(), context);
        }

        [JoltLibraryMethod("sum")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Sum(object? value, [OptionalParameter(default)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = ResolveValueWithOptionalProjection(value, lambda, context);

            if (resolved is null)
            {
                return context.CreateNullToken();
            }

            return AsIntegerOrFloatingPoint(resolved, x => x.Sum(), x => x.Sum(), context);
        }

        [JoltLibraryMethod("average")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Average(object? value, [OptionalParameter(default)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = ResolveValueWithOptionalProjection(value, lambda, context);

            if (resolved is null)
            {
                return context.CreateNullToken();
            }

            object? average = resolved switch
            {
                IEnumerable<int> integers => integers.Average(),
                IEnumerable<decimal> decimals => decimals.Average(),
                IEnumerable<double> doubles => doubles.Average(),
                IJsonArray array when array.ContainsAtLeastOneDecimal() => array.AsSequenceOf<double>().Average(),
                IJsonArray array when array.ContainsOnlyIntegers() => array.AsSequenceOf<long>().Average(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get average for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(average);
        }

        [JoltLibraryMethod("select")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Select(object? value, [LambdaVariables("x")] LambdaMethod lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var results = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda).Select(context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for #select with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(results);
        }

        [JoltLibraryMethod("distinct")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Distinct(object? value, [LambdaVariables("x")] LambdaMethod lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var results = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda).Distinct(context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for #distinct with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(results);
        }

        [JoltLibraryMethod("any")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Any(object? value, [OptionalParameter(default)] [LambdaVariables("x")] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateTokenFrom(false);
            }

            var empty = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda).Any(context),
                IJsonValue val when val.IsString() => lambda is null ? context.CreateTokenFrom(val.ToTypeOf<string>()?.Length > 0) : new QueryMethods(val.ToTypeOf<string>().ToCharArray().Select(x => context.CreateTokenFrom(x)), lambda).Any(context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("where")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Where(object? value, [LambdaVariables("x")] LambdaMethod lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var empty = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda).Where(context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("takeWhile")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? TakeWhile(object? value, [LambdaVariables("x")] LambdaMethod lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var takenItems = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda).TakeWhile(context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateArrayFrom(takenItems.ToArray());
        }

        [JoltLibraryMethod("take")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Take(object value, object amountToTake, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var resolvedAmount = context.ResolveArgumentOf<int>(context.ResolveValueOf(amountToTake, typeof(int)));

            var takenItems = resolved switch
            {
                IJsonArray array => new QueryMethods(array).Take(context, resolvedAmount),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateArrayFrom(takenItems.ToArray());
        }

        [JoltLibraryMethod("skipWhile")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? SkipWhile(object? value, [LambdaVariables("x")] LambdaMethod lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var takenItems = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda).SkipWhile(context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateArrayFrom(takenItems.ToArray());
        }

        [JoltLibraryMethod("skip")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Skip(object? value, object amountToSkip, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var resolvedAmount = context.ResolveValueOf(amountToSkip, typeof(long));

            if (resolvedAmount is null)
            {
                return context.CreateEmptyArray();
            }
            
            var takenItems = resolved switch
            {
                IJsonArray array => new QueryMethods(array).Skip(context, context.ResolveArgumentOf<long>(resolvedAmount)),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateArrayFrom(takenItems.ToArray());
        }

        [JoltLibraryMethod("keysFrom")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? KeysFrom(object? value, EvaluationContext context)
        {
            var resolved = (IJsonObject)context.ResolveValueOf<IJsonObject>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            return context.CreateArrayFrom(resolved.Select(k => context.CreateTokenFrom(k.PropertyName)).ToArray());
        }

        [JoltLibraryMethod("valuesFrom")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ValuesFrom(object? value, EvaluationContext context)
        {
            var resolved = (IJsonObject)context.ResolveValueOf<IJsonObject>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            return context.CreateArrayFrom(resolved.Select(k => k.Value).ToArray());
        }

        [JoltLibraryMethod("reduce")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Reduce(object? value, [LambdaVariables("acc", "current")] LambdaMethod lambda, [OptionalParameter(default)] object? seed, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateNullToken();
            }

            var query = resolved switch
            {
                IJsonArray array => new QueryMethods(array, lambda),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for reduce with unsupported object type '{value?.GetType()}'")
            };

            // When we don't have enough to reduce, just early out and ignore the lambda.

            var hasSeed = !(seed is null);
            var startingElements = query.Sequence.Take(2).ToArray();

            if (startingElements.Length == 0 || (startingElements.Length == 1 && !hasSeed))
            {
                return startingElements.FirstOrDefault();
            }

            object? seedValue = hasSeed ? context.ResolveValueOf<object>(seed) : seed;
            IJsonToken? seedToken = seedValue is IJsonToken token ? token : context.CreateTokenFrom(seedValue);
            IJsonToken? accumulator = hasSeed ? seedToken : startingElements[0];
            var sequence = hasSeed ? query.Sequence : query.Sequence.Skip(1);

            foreach (var item in sequence)
            {
                accumulator = query.ExecuteLambda(accumulator, item, context);
            }

            return accumulator;
        }

        [JoltLibraryMethod("zip")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Zip(object? first, object? second, [LambdaVariables("x", "y")] LambdaMethod lambda, EvaluationContext context)
        {
            var resolvedFirst = context.ResolveValueOf<IJsonArray>(first);
            var resolvedSecond = context.ResolveValueOf<IJsonArray>(second);

            if (resolvedFirst is null || resolvedSecond is null)
            {
                return context.CreateEmptyArray();
            }

            var zipped = (resolvedFirst, resolvedSecond) switch
            {
                (IJsonArray array1, IJsonArray array2) => new QueryMethods(array1, lambda).Zip(array2, context),
                _ => throw new ArgumentOutOfRangeException(nameof(first), $"Unable to check contents for zip with unsupported object type '{first?.GetType()}'")
            };

            return context.CreateArrayFrom(zipped.ToArray());
        }

        [JoltLibraryMethod("summarizeWith")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? SummarizeWith(object? value, [LambdaVariables("group")] LambdaMethod lambda, EvaluationContext context)
        {
            IJsonObject? ConvertToGroup(IJsonToken? groupToken)
            {
                var keyToken = groupToken?.AsObject()["key"];
                var resultsToken = groupToken?.AsObject()["results"].AsArray();

                return context.CreateTokenFrom(new Dictionary<string, IJsonToken?>
                {
                    { "key", keyToken },
                    { "results", resultsToken }
                }).AsObject();
            }

            IEnumerable<IJsonToken> CreateSummaryFor(IJsonArray array)
            {
                foreach (var group in array.Select(x => ConvertToGroup(x)))
                {
                    var token = context.CreateTokenFrom(new object()).AsObject();

                    var result = new QueryMethods(lambda).ExecuteLambda(group, context);

                    token["key"] = group?["key"];
                    token["value"] = result;

                    yield return token;
                }
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);

            if (resolved is null)
            {
                return context.CreateEmptyArray();
            }

            var resultToken = context.CreateTokenFrom(resolved);

            resultToken = resultToken switch
            {
                IJsonArray array when array.IsGroup() => context.CreateArrayFrom(CreateSummaryFor(array).ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to summarize with non-group type '{value.GetType()}'")
            };

            return resultToken;
        }

        private static object? ResolveValueWithOptionalProjection(object? value, LambdaMethod? projection, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            // A missing value should stay missing rather than being projected into an empty array.

            if (resolved is null || projection is null)
            {
                return resolved;
            }

            return Select(resolved, projection, context);
        }

        private static IJsonToken? AsIntegerOrFloatingPoint(object? value, Func<IEnumerable<long>, long?> asInt64, Func<IEnumerable<double>, double> asDecimal, EvaluationContext context)
        {
            // We're separating out the integer sum from the floating point so we won't
            // potentially get a rounding representation error by always defaulting to double.

            var integerValue = value switch
            {
                IEnumerable<int> integers => asInt64(integers.Cast<long>()),
                IEnumerable<long> integers => asInt64(integers),
                IJsonArray array when array.ContainsOnlyIntegers() => asInt64(array.AsSequenceOf<long>()),
                _ => null
            };

            if (integerValue != null)
            {
                return context.CreateTokenFrom(integerValue);
            }

            var decimalValue = value switch
            {
                IEnumerable<double> doubles => asDecimal(doubles),
                IJsonArray array when array.ContainsOnlyNumbers() => asDecimal(array.Select(x => x.AsValue().ToTypeOf<double>())),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get integer or floating point value for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(decimalValue);
        }
    }
}
