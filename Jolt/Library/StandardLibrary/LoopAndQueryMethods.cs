using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal sealed class LoopAndQueryMethods
    {
        [JoltLibraryMethod("foreach", true)]
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
                string path => context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromClosestMatch),
                RangeVariable variable => variable.Value,
                DereferencedPath path when path.MissingPaths.Any() => throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.AttemptedToDereferenceMissingPath, path.MissingPaths.Join('.'), path.ObtainableToken.PropertyName),
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
        public static IJsonToken? GroupBy(object? value, LambdaMethod keySelectorLambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var grouping = resolved switch
            {
                IJsonArray array => array.GroupBy(x => ProjectAs(x, keySelectorLambda, context)?.ToTypeOf<object>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform a group by using unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("orderBy")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderBy(object? value, [OptionalParameter(null)] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var grouping = resolved switch
            {
                IJsonArray array when array.ContainsOnlyNumbers() && array.ContainsAtLeastOneDecimal() => array.OrderBy(x => x.ToTypeOf<double>()),
                IJsonArray array when array.ContainsOnlyNumbers() => array.OrderBy(x => x.ToTypeOf<long>()),
                IJsonArray array => array.OrderBy(x => ProjectAs(x, lambda, context)?.ToTypeOf<object>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform an order by using unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("orderByDesc")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderByDescending(object? value, [OptionalParameter(null)] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var grouping = resolved switch
            {
                IJsonArray array when array.ContainsOnlyNumbers() && array.ContainsAtLeastOneDecimal() => array.OrderByDescending(x => x.ToTypeOf<double>()),
                IJsonArray array when array.ContainsOnlyNumbers() => array.OrderByDescending(x => x.ToTypeOf<long>()),
                IJsonArray array => array.OrderByDescending(x => ProjectAs(x, lambda, context)?.ToTypeOf<object>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform an order by using unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("max")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Maximum(object? value, [OptionalParameter(default)] LambdaMethod? lambda, EvaluationContext context)
        {
            if (value is RangeVariable variable)
            {
                value = variable.Value;

                if (lambda != null)
                {
                    value = Select(value, lambda, context);
                }
            }

            return AsIntegerOrFloatingPoint(value, x => x.Max(), x => x.Max(), context);
        }

        [JoltLibraryMethod("min")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Minimum(object? value, [OptionalParameter(default)] LambdaMethod? lambda, EvaluationContext context)
        {
            if (value is RangeVariable variable)
            {
                value = variable.Value;

                if (lambda != null)
                {
                    value = Select(value, lambda, context);
                }
            }

            return AsIntegerOrFloatingPoint(value, x => x.Min(), x => x.Min(), context);
        }

        [JoltLibraryMethod("sum")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Sum(object? value, [OptionalParameter(default)] LambdaMethod? lambda, EvaluationContext context)
        {
            if (value is RangeVariable variable)
            {
                value = variable.Value;

                if (lambda != null)
                {
                    value = Select(value, lambda, context);
                }
            }

            return AsIntegerOrFloatingPoint(value, x => x.Sum(), x => x.Sum(), context);
        }

        [JoltLibraryMethod("average")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Average(object? value, [OptionalParameter(default)] LambdaMethod? lambda, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            if (value is RangeVariable variable)
            {
                value = variable.Value;

                if (lambda != null)
                {
                    resolved = Select(value, lambda, context);
                }
                else
                {
                    resolved = value;
                }
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
        public static IJsonToken? Select(object? value, LambdaMethod lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var empty = resolved switch
            {
                IJsonArray array => LambdaOrDefault(array, lambda, Select, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("any")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Any(object? value, [OptionalParameter(default)] LambdaMethod? lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var empty = resolved switch
            {
                IJsonArray array => LambdaOrDefault<IJsonToken, IJsonToken>(array, lambda, Any, context, () => context.CreateTokenFrom(array.Length > 0)),
                IJsonValue val when val.IsString() => LambdaOrDefault<char, IJsonToken>(val.ToTypeOf<string>(), lambda, Any, context, () => context.CreateTokenFrom(val.ToTypeOf<string>().Length > 0)),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("where")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Where(object? value, LambdaMethod lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var empty = resolved switch
            {
                IJsonArray array => LambdaOrDefault(array, lambda, Where, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("takeWhile")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? TakeWhile(object? value, LambdaMethod lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateArrayFrom(Array.Empty<IJsonToken>());
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var takenItems = resolved switch
            {
                IJsonArray array => TakeWhile(array, lambda, x => ExecuteLambdaBody(x, context)?.ToTypeOf<bool>() == true, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateArrayFrom(takenItems.ToArray());
        }

        [JoltLibraryMethod("skipWhile")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? SkipWhile(object? value, LambdaMethod lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateArrayFrom(Array.Empty<IJsonToken>());
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var takenItems = resolved switch
            {
                IJsonArray array => SkipWhile(array, lambda, x => ExecuteLambdaBody(x, context)?.ToTypeOf<bool>() == true, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateArrayFrom(takenItems.ToArray());
        }

        [JoltLibraryMethod("summarizeWith")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? SummarizeWith(object? value, LambdaMethod lambda, EvaluationContext context)
        {
            (object Key, IJsonArray? Results) ConvertToGroup(IJsonToken? groupToken)
            {
                var keyToken = groupToken?.AsObject()["key"];
                var resultsToken = groupToken?.AsObject()["results"].AsArray();

                if (keyToken is null || resultsToken is null)
                {
                    return (default, context.CreateArrayFrom(Array.Empty<IJsonToken>()).AsArray());
                }

                return (keyToken.ToTypeOf<object>(), resultsToken);
            }

            IEnumerable<IJsonToken> CreateSummaryFor(IJsonArray array)
            {
                foreach (var group in array.Select(x => ConvertToGroup(x)))
                {
                    var token = context.CreateTokenFrom(new object()).AsObject();

                    var result = ProjectAs(group.Results, lambda, context);

                    token[group.Key?.ToString()] = result;

                    yield return token;
                }
            }

            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }

            var resolved = context.ResolveValueOf<IJsonArray>(value);
            var resultToken = context.CreateTokenFrom(resolved);

            resultToken = resultToken switch
            {
                IJsonArray array when array.IsGroup() => context.CreateArrayFrom(CreateSummaryFor(array).ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to summarize with non-group type '{value.GetType()}'")
            };

            return resultToken;
        }

        private static IJsonToken? AsIntegerOrFloatingPoint(object? value, Func<IEnumerable<long>, long?> asInt64, Func<IEnumerable<double>, double> asDecimal, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            // We're separating out the integer sum from the floating point so we won't
            // potentially get a rounding representation error by always defaulting to double.

            var integerValue = resolved switch
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
                IEnumerable<decimal> decimals => asDecimal(decimals.Cast<double>()),
                IEnumerable<double> doubles => asDecimal(doubles),
                IJsonArray array when array.ContainsOnlyNumbers() => asDecimal(array.Select(x => x.AsValue().ToTypeOf<double>())),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get integer or floating point value for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(decimalValue);
        }

        private static IJsonToken? ExecuteLambdaBody(Expression lambdaBodyExpression, EvaluationContext context)
        {
            var evaluationContext = new EvaluationContext(
                context.Mode,
                lambdaBodyExpression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform);

            var result = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

            return result.TransformedToken;
        }

        private static IJsonToken? LambdaOrDefault<T, TResult>(IEnumerable<T> sequence, LambdaMethod? lambda, Func<IEnumerable<T>, LambdaMethod, Func<Expression, IJsonToken?>, EvaluationContext, IJsonToken?> applyToItems, EvaluationContext context, Func<TResult> useDefault)
        {
            if (lambda is null)
            {
                if (useDefault is null)
                {
                    return default;
                }

                var result = useDefault();

                return context.CreateTokenFrom(result);
            }

            return applyToItems(sequence, lambda, body => ExecuteLambdaBody(lambda.Body, context), context);
        }

        private static IJsonToken? LambdaOrDefault<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<IEnumerable<T>, LambdaMethod, Func<Expression, IJsonToken?>, EvaluationContext, IEnumerable<IJsonToken>> applyToItems, EvaluationContext context)
        {
            var results = applyToItems(sequence, lambda, body => ExecuteLambdaBody(lambda.Body, context), context).ToArray();

            return context.CreateArrayFrom(results);
        }

        private static IEnumerable<IJsonToken> TakeWhile<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<Expression, bool> shouldInclude, EvaluationContext context)
        {
            foreach (var item in sequence)
            {
                var itemToken = context.CreateTokenFrom(item);
                var loopVariable = new RangeVariable(lambda.Variable.Name, itemToken);

                context.Scope.AddOrUpdateVariable(loopVariable);

                try
                {
                    if (shouldInclude(lambda.Body))
                    {
                        yield return itemToken;
                    }
                    else
                    {
                        yield break;
                    }
                }
                finally
                {
                    context.Scope.RemoveCurrentVariablesLayer();
                }
            }
        }

        private static IEnumerable<IJsonToken> SkipWhile<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<Expression, bool> shouldSkip, EvaluationContext context)
        {
            var isSkipping = true;

            foreach (var item in sequence)
            {
                var itemToken = context.CreateTokenFrom(item);
                var loopVariable = new RangeVariable(lambda.Variable.Name, itemToken);

                context.Scope.AddOrUpdateVariable(loopVariable);

                try
                {
                    if (isSkipping && shouldSkip(lambda.Body))
                    {
                        continue;
                    }

                    isSkipping = false;

                    yield return itemToken;
                }
                finally
                {
                    context.Scope.RemoveCurrentVariablesLayer();
                }
            }
        }


        private static IEnumerable<IJsonToken> ProjectInto<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<Expression, bool> shouldInclude, Func<IJsonToken, IJsonToken> createProjection, EvaluationContext context)
        {
            foreach (var item in sequence)
            {
                var itemToken = context.CreateTokenFrom(item);
                var loopVariable = new RangeVariable(lambda.Variable.Name, itemToken);

                context.Scope.AddOrUpdateVariable(loopVariable);

                try
                {
                    if (shouldInclude(lambda.Body))
                    {
                        yield return createProjection(itemToken);
                    }
                }
                finally
                {
                    context.Scope.RemoveCurrentVariablesLayer();
                }
            }
        }

        private static IJsonToken? ProjectAs<T>(T value, LambdaMethod lambda, EvaluationContext context)
        {
            var itemToken = context.CreateTokenFrom(value);
            var loopVariable = new RangeVariable(lambda.Variable.Name, itemToken);

            context.Scope.AddOrUpdateVariable(loopVariable);

            try
            {
                return ExecuteLambdaBody(lambda.Body, context);
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
        }

        private static IEnumerable<IJsonToken> Select<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<Expression, IJsonToken> getSelection, EvaluationContext context)
        {
            return ProjectInto(sequence, lambda, x => true, x => ExecuteLambdaBody(lambda.Body, context), context);
        }

        private static IEnumerable<IJsonToken> Where<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<Expression, IJsonToken> isMatch, EvaluationContext context)
        {
            return ProjectInto(sequence, lambda, x => isMatch(x).ToTypeOf<bool>(), x => x, context);
        }

        private static IJsonToken Any<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<Expression, IJsonToken> isMatch, EvaluationContext context)
        {
            var isAny = Where(sequence, lambda, isMatch, context).Any();

            return context.CreateTokenFrom(isAny);
        }
    }
}
