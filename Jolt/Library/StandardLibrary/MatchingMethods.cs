using Jolt.Evaluation;
using Jolt.Evaluation.Matching;
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
    internal sealed class MatchingMethods
    {
        [JoltLibraryMethod("match", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? Match(VariableAlias sourceAlias, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array))
            {
                return null;
            }

            var closestViableSourceToken = sourceAlias.Source switch
            {
                string path => context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot),
                RangeVariable rangeVariable => rangeVariable.Value,
                _ => throw context.CreateExecutionErrorFor<MatchingMethods>(ExceptionCode.UnableToPerformUsingLibraryCallDueToInvalidParameter, sourceAlias.Source)
            };

            var matchVariable = new RangeVariable(sourceAlias.Variable.Name, closestViableSourceToken.Copy());

            context.Scope.AddOrUpdateVariable(matchVariable);

            try
            {
                foreach (var caseBlock in token.AsArray())
                {
                    // We're introducing a new variables layer dedicated to any variables that show up within
                    // the evaluation of a given case block. We don't want them to bleed over into other cases,
                    // so we're not putting these in the match variable scope.

                    context.Scope.AddEmptyVariablesLayer();

                    try
                    {
                        if (caseBlock.Type != JsonTokenType.Object)
                        {
                            throw context.CreateExecutionErrorFor<MatchingMethods>(ExceptionCode.MatchCaseBlockMustBeAnObject);
                        }

                        var caseProperty = caseBlock.AsObject().FirstOrDefault();

                        if (caseProperty is null)
                        {
                            throw context.CreateExecutionErrorFor<MatchingMethods>(ExceptionCode.MatchCaseBlockMustBeAnObjectWithExactlyOneProperty);
                        }

                        var propertyNameToken = new EvaluationToken(
                            caseProperty.PropertyName,
                            default,
                            token,
                            default,
                            default,
                            parentRangeVariable: matchVariable,
                            isWithinMatchBlock: true);

                        var result = context.Transform(propertyNameToken, context.Scope);

                        if (result.Type != JsonTokenType.Value || result.AsValue().ValueType != JsonValueType.Boolean)
                        {
                            throw context.CreateExecutionErrorFor<MatchingMethods>(ExceptionCode.UnableToEvaluateResultOfMatchCaseExpressionDueToNonBooleanResult);
                        }

                        var isMatch = result.AsValue().ToTypeOf<bool>();

                        if (isMatch)
                        {
                            var valueExpression = caseProperty.Value.ToTypeOf<string>();
                            var actualTokens = context.JsonContext.TokenReader.ReadToEnd(valueExpression, EvaluationMode.PropertyValue);

                            if (!context.JsonContext.ExpressionParser.TryParseExpression(actualTokens, context.JsonContext, out var expression))
                            {
                                throw context.CreateExecutionErrorFor<MatchingMethods>(ExceptionCode.UnableToParseMatchCaseBlockValueExpression, valueExpression);
                            }

                            var propertyValueToken = new EvaluationToken(
                                context.Token.PropertyName,
                                context.Token.ResolvedPropertyName,
                                default,
                                caseProperty.Value);

                            var evaluationContext = new EvaluationContext(
                                EvaluationMode.PropertyValue,
                                expression,
                                context.JsonContext,
                                propertyValueToken,
                                context.Scope,
                                context.Transform);

                            var caseResult = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

                            return caseResult.TransformedToken;
                        }
                    }
                    finally
                    {
                        context.Scope.RemoveCurrentVariablesLayer();
                    }
                }

                return context.CreateTokenFrom(null);
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
        }

        [JoltLibraryMethod("is", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.MatchBlock)]
        public static IJsonToken? IsType(object typeToCheckAgainst, EvaluationContext context)
        {
            var implicitMatchValue = context.Token.ParentRangeVariable?.Value;
            var isMatched = false;
            
            if (typeToCheckAgainst is MatchCaseType type)
            {
                isMatched = type switch
                {
                    MatchCaseType.Array => implicitMatchValue?.Type == JsonTokenType.Array,
                    MatchCaseType.Object => implicitMatchValue?.Type == JsonTokenType.Object,
                    MatchCaseType.Null => implicitMatchValue?.Type == JsonTokenType.Null || implicitMatchValue == null,
                    _ => false
                };

                if (!isMatched && implicitMatchValue?.Type == JsonTokenType.Value)
                {
                    var valueType = implicitMatchValue.AsValue().ValueType;

                    isMatched = type switch
                    {
                        MatchCaseType.Boolean => valueType == JsonValueType.Boolean,
                        MatchCaseType.Decimal => valueType == JsonValueType.Number && implicitMatchValue.ToTypeOf<string>().Contains('.'),
                        MatchCaseType.Integer => valueType == JsonValueType.Number && !implicitMatchValue.ToTypeOf<string>().Contains('.'),
                        MatchCaseType.String => valueType == JsonValueType.String,
                        _ => false
                    };
                }
            }
            else if (typeToCheckAgainst is IJsonObject patternObject && implicitMatchValue is IJsonObject matchObject)
            {
                var hasAnyProperties = patternObject.Any();

                if (hasAnyProperties)
                {
                    isMatched = IsObjectSubsetPropertyMatch(patternObject, matchObject, context);
                }
                else
                {
                    isMatched = true;
                }
            }
            else if (typeToCheckAgainst is IJsonArray patternArray && implicitMatchValue is IJsonArray matchArray)
            {
                isMatched = IsArraySubsetPropertyMatch(patternArray, matchArray, context);
            }
            else
            {
                isMatched = false;
            }

            return context.CreateTokenFrom(isMatched);
        }

        private static bool IsArraySubsetPropertyMatch(IJsonArray? patternArray, IJsonArray? matchArray, EvaluationContext context)
        {
            bool HasMatches() => patternArray.Length > 0;
            bool IsIndexBeyondEnd(int index) => !HasMatches() || index > patternArray.Length - 1;

            bool IsDiscard(IJsonToken? token) => token != null && token.Type == JsonTokenType.Object && token.AsObject().ToTypeOf<MatchDiscard>()?.Kind == MatchKind.Discard;
            bool IsVariableDiscard(IJsonToken? token) => token != null && token.Type == JsonTokenType.Object && token.AsObject().ToTypeOf<MatchDiscard>()?.Kind == MatchKind.Variable;
            bool IsDiscardAtPosition(int index) => IsIndexBeyondEnd(index) ? IsDiscard(patternArray[^1]) : IsDiscard(patternArray[index]);
            bool IsVariableDiscardAtPosition(int index) => IsIndexBeyondEnd(index) ? IsVariableDiscard(patternArray[^1]) : IsVariableDiscard(patternArray[index]);
            bool IsDiscardAtEnd() => HasMatches() && IsDiscardAtPosition(patternArray.Length - 1);
            int GetNumberOfRequiredSingleElements() => IsDiscardAtEnd() ? patternArray.Length - 1 : patternArray.Length;

            bool IsEqual(IJsonToken patternToken, IJsonToken value) => IsDiscard(patternToken) || patternToken.DeepEquals(value);
            bool IsEqualAtPosition(int index) => IsEqual(patternArray[index], matchArray[index]);

            bool IsPatternLongerThanMatch() => patternArray.Length > matchArray.Length;
            bool IsPatternShorterThanMatch() => patternArray.Length < matchArray.Length;
            bool IsPatternSameLengthAsMatch() => patternArray.Length == matchArray.Length;

            bool IsMatchable() =>
                IsPatternSameLengthAsMatch() ||
                (IsPatternLongerThanMatch() && GetNumberOfRequiredSingleElements() == patternArray.Length - 1 && IsDiscardAtEnd()) ||
                (IsPatternShorterThanMatch() && IsDiscardAtEnd());

            bool ArrayPatternsMatchArrayContents()
            {
                var requiredMatchAttempts = GetNumberOfRequiredSingleElements();

                for (var i = 0; i < requiredMatchAttempts; i++)
                {
                    if (!IsDiscardAtPosition(i) && !IsEqualAtPosition(i))
                    {
                        if (IsVariableDiscardAtPosition(i))
                        {
                            var discard = patternArray[i].AsObject().ToTypeOf<MatchDiscard>();

                            context.Scope.AddOrUpdateVariable(new RangeVariable(discard.VariableName, matchArray[i]), forceApplyToCurrentLayer: true);

                            continue;
                        }

                        return false;
                    }
                }

                return true;
            }

            if (patternArray is null ||  matchArray is null)
            {
                return false;
            }

            if (!IsMatchable())
            {
                return false;
            }

            return patternArray.Length switch
            {
                0 => matchArray.Length == 0,
                1 when IsDiscardAtEnd() => matchArray.Length == 1,
                1 => matchArray.Length == 1 && IsEqualAtPosition(0),
                _ => ArrayPatternsMatchArrayContents()
            };
        }

        private static bool IsPropertyValueSameType(IJsonToken? patternToken, IJsonToken? matchToken)
        {
            if (patternToken is null && matchToken is null)
            {
                return true;
            }

            if (patternToken is null || matchToken is null)
            {
                return false;
            }

            if (patternToken.Type != matchToken.Type)
            {
                return false;
            }

            if (patternToken.Type == JsonTokenType.Value)
            {
                return patternToken.AsValue().ValueType == matchToken.AsValue().ValueType;
            }

            return true;
        }

        private static bool IsPropertyValueMatch(IJsonValue patternValue, IJsonValue matchValue)
        {
            bool IsDecimal() => patternValue.ToTypeOf<string>().Contains(".") && matchValue.ToTypeOf<string>().Contains(".");

            return (patternValue.ValueType, matchValue.ValueType) switch
            {
                (JsonValueType.String, JsonValueType.String) => patternValue.ToTypeOf<string>() == matchValue.ToTypeOf<string>(),
                (JsonValueType.Boolean, JsonValueType.Boolean) => patternValue.ToTypeOf<bool>() == matchValue.ToTypeOf<bool>(),
                (JsonValueType.Null, JsonValueType.Null) => true,
                (JsonValueType.Number, JsonValueType.Number) when IsDecimal() => patternValue.ToTypeOf<double>() == matchValue.ToTypeOf<double>(),
                (JsonValueType.Number, JsonValueType.Number) => patternValue.ToTypeOf<long>() == matchValue.ToTypeOf<long>(),
                _ => false
            };
        }

        private static bool IsObjectSubsetPropertyMatch(IJsonObject patternObject, IJsonObject matchObject, EvaluationContext context)
        {
            foreach (var property in patternObject)
            {
                if (!matchObject.HasProperty(property.PropertyName))
                {
                    return false;
                }

                var matchValue = matchObject[property.PropertyName];

                // Is this a variable-style discard pattern? Let's check on that before we try any evaluations.

                if (property.Value?.Type == JsonTokenType.Object)
                {
                    var discard = property.Value.ToTypeOf<MatchDiscard>();

                    if (discard.Kind == MatchKind.Variable && !string.IsNullOrWhiteSpace(discard.VariableName))
                    {
                        context.Scope.AddOrUpdateVariable(new RangeVariable(discard.VariableName, matchValue), forceApplyToCurrentLayer: true);

                        continue;
                    }
                    else if (discard.Kind == MatchKind.Discard)
                    {
                        continue;
                    }
                }

                if (!IsPropertyValueSameType(property.Value, matchValue))
                {
                    return false;
                }

                var isPropertyValueMatched = property.Value?.Type switch
                {
                    JsonTokenType.Object => IsObjectSubsetPropertyMatch(property.Value.AsObject(), matchValue.AsObject(), context),
                    JsonTokenType.Array => IsArraySubsetPropertyMatch(property.Value.AsArray(), matchValue.AsArray(), context),
                    JsonTokenType.Null when matchValue.IsValue() && matchValue.AsValue().ValueType == JsonValueType.Null => true,
                    JsonTokenType.Value when matchValue.IsValue() => IsPropertyValueMatch(property.Value.AsValue(), matchValue.AsValue()),
                    _ => false
                };

                if (!isPropertyValueMatched)
                {
                    return false;
                }
            }

            return true;
        }

        [JoltLibraryMethod("default", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.MatchBlock)]
        public static IJsonToken? Default(EvaluationContext context)
        {
            return context.CreateTokenFrom(true);
        }

        [JoltLibraryMethod("given", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.MatchBlock)]
        public static IJsonToken? Given([LazyEvaluation] Expression expression, EvaluationContext context)
        {
            // The 'given' method is a special case within pattern matching. It has to appear on the property name side
            // of the match case but it also needs to allow regular methods and operations that would ultimately
            // return a boolean value. To allow this, we're pretending we're on the property value side for the
            // purposes of this expression evaluation.

            var evaluationContext = new EvaluationContext(
                EvaluationMode.PropertyValue,
                expression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform
            );

            var result = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

            if (result.TransformedToken.Type != JsonTokenType.Value || result.TransformedToken.AsValue().ValueType != JsonValueType.Boolean)
            {
                throw context.CreateExecutionErrorFor<MatchingMethods>(ExceptionCode.UnableToEvaluateResultOfGivenExpressionDueToNonBooleanResult);
            }

            return result.TransformedToken;
        }
    }
}
