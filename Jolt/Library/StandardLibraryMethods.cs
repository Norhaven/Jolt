using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Jolt.Library
{
    internal class StandardLibraryMethods
    {
        [JoltLibraryMethod("valueOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ValueOf(string path, EvaluationContext context)
        {
            // ValueOf will always start a search from the root, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot);
        }

        [JoltLibraryMethod("exists")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Exists(object? pathOrValue, EvaluationContext context)
        {
            var valueExists = pathOrValue != null;

            if (pathOrValue is IJsonToken token)
            {
                var tokenExists = valueExists && token.Type != JsonTokenType.Null && token.AsValue().ToTypeOf<object>() != null;

                return context.CreateTokenFrom(tokenExists);
            }

            if (pathOrValue is string path && context.JsonContext.QueryPathProvider.IsQueryPath(path))
            {
                var tokenValue = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot);

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
                context.Scope,
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
                _ => throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToCompleteIncludeIfLibraryCallDueToNonBooleanCondition, result)
            };

            if (!shouldInclude)
            {
                return default;
            }

            var includedJson = context.Token.CurrentTransformerToken;
            var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;
            var currentSource = new SourceToken(0, default);

            var evaluationToken = new EvaluationToken(propertyName, default, includedJson.Parent, includedJson, currentSource);

            return context.Transform(evaluationToken, context.Scope);
        }

        [JoltLibraryMethod("eval")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static EvaluationResult? Evaluate(string pathOrLiteral, EvaluationContext context)
        {
            var actualTokens = context.JsonContext.TokenReader.ReadToEnd(pathOrLiteral, context.Mode);

            if (!context.JsonContext.ExpressionParser.TryParseExpression(actualTokens, context.JsonContext, out var expression))
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToParseEvalLibraryCallExpression, pathOrLiteral);
            }

            var evaluationContext = new EvaluationContext(
                context.Mode,
                expression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform);

            return context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        [JoltLibraryMethod("foreach", true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IEnumerable<IJsonToken> LoopOnArrayOrObjectAtPath(Enumeration enumeration, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object))
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableToken, token.Type);
            }

            var closestViableSourceToken = enumeration.Source switch
            {                
                string path => context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromClosestMatch),
                RangeVariable variable => variable.Value,
                _ => throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallDueToInvalidParameter, enumeration.Source)
            };

            if (closestViableSourceToken?.Type.IsAnyOf(JsonTokenType.Array, JsonTokenType.Object) != true)
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableSourceToken, closestViableSourceToken?.Type);
            }

            var contentTemplate = token.Type switch
            { 
                JsonTokenType.Array => token.AsArray().RemoveAt(0),
                JsonTokenType.Object => token.AsObject().Copy(),
                _ => throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallDueToMissingContentTemplate, token.Type)
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
                    throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableSourceToken, closestViableSourceToken.Type);
                }
            }

            return EnumerateClosestSourceToken();
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
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => token.AsValue().ToTypeOf<string>().Length,
                object[] array => array.Length,
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get length for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(length);
        }

        [JoltLibraryMethod("substring")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Substring(object? value, Range range, EvaluationContext context)
        {   
            static string AsString(IJsonValue token) => token.AsValue().ToTypeOf<string>();
            
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
        public static IJsonToken? OrderBy(object? value, [OptionalParameter(null)] string? propertyName, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var grouping = resolved switch
            {
                IJsonArray array when array.ContainsOnlyNumbers() && array.ContainsAtLeastOneDecimal() => array.OrderBy(x => x.ToTypeOf<double>()),
                IJsonArray array when array.ContainsOnlyNumbers() => array.OrderBy(x => x.ToTypeOf<long>()),
                IJsonArray array => array.OrderBy(x => ((IJsonObject)x)[propertyName]?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to order by '{propertyName}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("orderByDesc")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? OrderByDescending(object? value, [OptionalParameter(null)] string? propertyName, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            var grouping = resolved switch
            {
                IJsonArray array when array.ContainsOnlyNumbers() && array.ContainsAtLeastOneDecimal() => array.OrderByDescending(x => x.ToTypeOf<double>()),
                IJsonArray array when array.ContainsOnlyNumbers() => array.OrderByDescending(x => x.ToTypeOf<long>()),
                IJsonArray array => array.OrderByDescending(x => ((IJsonObject)x)[propertyName]?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to group by '{propertyName}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(grouping);
        }

        [JoltLibraryMethod("contains")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Contains(object? instance, object? value, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(instance);

            var contains = resolved switch
            {
                string text when value is string valueText => text.Contains(valueText),
                IJsonArray array => array.Contains(context.CreateTokenFrom(value)),
                IJsonValue token when token.ValueType == JsonValueType.String => token.AsValue().ToTypeOf<string>().Contains(value?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine contents for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(contains);
        }

        [JoltLibraryMethod("roundTo")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? RoundTo(object? value, object? decimalPlaces, EvaluationContext context)
        {
            var resolved = context.ResolveQueryPathIfPresent(value);

            object? rounded = resolved switch
            {
                long i => i,
                double d when decimalPlaces is long i => Math.Round(d, (int)i),
                double d when decimalPlaces is IJsonValue token && token.ValueType == JsonValueType.Number => Math.Round(d, token.ToTypeOf<int>()),
                decimal d when decimalPlaces is long i => Math.Round(d, (int)i),
                decimal d when decimalPlaces is IJsonValue token && token.ValueType == JsonValueType.Number => Math.Round(d, token.ToTypeOf<int>()),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is long i => Math.Round(token.ToTypeOf<decimal>(), (int)i),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is IJsonValue val && val.ValueType == JsonValueType.Number => Math.Round(token.ToTypeOf<decimal>(), val.ToTypeOf<int>()),
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


            object? average = resolved switch
            {
                IEnumerable<int> integers => integers.Average(),
                IEnumerable<decimal> decimals => decimals.Average(),
                IEnumerable<double> doubles => doubles.Average(),
                IJsonArray array when array.ContainsAtLeastOneDecimal() => array.AsSequenceOf<double>().Average(),
                IJsonArray array when array.ContainsOnlyIntegers() => array.AsSequenceOf<double>().Average(),
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
                IJsonArray array when array.ContainsOnlyStrings() => string.Join(delimiter, array.Select(x => x.AsValue().ToTypeOf<string>())),
                IJsonArray array => string.Join(delimiter, array.Select(x => x.AsValue().ToTypeOf<object>()?.ToString())),
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

            return context.CreateTokenFrom(resolved is int || resolved is long || ((resolved is double || resolved is decimal) && !resolved.ToString().Contains(".")));
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

            return context.CreateTokenFrom(resolved is decimal || resolved is double);
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
        public static IJsonToken? Any(object? value, [OptionalParameter(default)] LambdaMethod? lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }
            
            var resolved = context.ResolveQueryPathIfPresent(value);

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

            var resolved = context.ResolveQueryPathIfPresent(value);

            var empty = resolved switch
            {
                IJsonArray array => LambdaOrDefault(array, lambda, Where, context),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check contents for any with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("select")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Select(object? value, LambdaMethod lambda, EvaluationContext context)
        {
            if (value is null)
            {
                return context.CreateTokenFrom(false);
            }

            var resolved = context.ResolveQueryPathIfPresent(value);

            var empty = resolved switch
            {
                IJsonArray array => LambdaOrDefault(array, lambda, Select, context),
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

        [JoltLibraryMethod("removeAt")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue | LibraryMethodTarget.StatementBlock)]
        public static IJsonToken? RemoveAt(DereferencedPath path, EvaluationContext context)
        {
            // Ensure that non-scoped variables outside of the 'using' block aren't able to be
            // modified just because we're in statement mode for some other use.

            if (!context.Scope.ContainsVariable(path.SourceVariable.Name))
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.AttemptedToIndirectlyModifyVariableWithinUsingBlock, path.SourceVariable.Name);
            }

            if (path.MissingPaths.Length > 0)
            {
                var missing = string.Join('.', path.MissingPaths);
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.AttemptedToDereferenceMissingPath, missing, path.ObtainableToken.PropertyName);
            }

            var token = path.ObtainableToken;

            if (token.Parent is IJsonObject obj)
            {
                obj.Remove(token.PropertyName);
            }
            else if (token.Parent is IJsonProperty property)
            {
                ((IJsonObject)property.Parent).Remove(token.PropertyName);
            }
            else
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToRemoveNodeFromNonObjectParent, token.PropertyName);
            }

            return token;
        }

        [JoltLibraryMethod("setAt")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue | LibraryMethodTarget.StatementBlock)]
        public static IJsonToken? SetAt(DereferencedPath path, object? newValue, EvaluationContext context)
        {
            // Ensure that non-scoped variables outside of the 'using' block aren't able to be
            // modified just because we're in statement mode for some other use.

            if (!context.Scope.ContainsVariable(path.SourceVariable.Name))
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.AttemptedToIndirectlyModifyVariableWithinUsingBlock, path.SourceVariable.Name);
            }

            var token = path.ObtainableToken;

            var actualNewValue = newValue switch
            {
                DereferencedPath pathValue when pathValue.MissingPaths.Length == 0 => pathValue.ObtainableToken,
                DereferencedPath pathValue => throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.AttemptedToDereferenceMissingPath, string.Join('.', pathValue.MissingPaths), pathValue.ObtainableToken.PropertyName),
                string pathValue => context.ResolveQueryPathIfPresent(pathValue) as IJsonToken,
                object obj => context.CreateTokenFrom(obj),
                null => context.CreateTokenFrom(null)
            };

            var current = token as IJsonObject;

            if (current is null)
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToSetPropertyOnNonObjectReference, token.PropertyName);
            }

            var missingPath = string.Join('.', path.MissingPaths);

            current.AddAtPath(missingPath, actualNewValue);

            return token;
        }

        [JoltLibraryMethod("using", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? Using(VariableAlias variable, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array))
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformUsingLibraryCallOnNonArrayToken, token.Type);
            }

            var closestViableSourceToken = variable.Source switch
            {
                string path => context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot),
                RangeVariable rangeVariable => rangeVariable.Value,
                _ => throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformUsingLibraryCallDueToInvalidParameter, variable.Source)
            };

            if (closestViableSourceToken?.Type != JsonTokenType.Object)
            {
                throw context.CreateExecutionErrorFor<StandardLibraryMethods>(ExceptionCode.UnableToPerformUsingLibraryCallOnNonObjectToken, closestViableSourceToken?.Type);
            }

            var statements = token switch
            {
                IJsonArray array => array.Copy().AsArray(),
                _ => throw new ArgumentOutOfRangeException(nameof(variable), $"Unable to locate array containing statements for 'using' block")
            };

            token.Clear();

            var loopVariable = new RangeVariable(variable.Variable.Name, closestViableSourceToken.Copy());

            context.Scope.AddOrUpdateVariable(loopVariable);

            try
            {
                foreach (var statement in statements)
                {
                    var currentEvaluationToken = new EvaluationToken(context.Token.PropertyName, context.Token.ResolvedPropertyName, token, statement, default, true, isWithinStatementBlock: true);

                    context.Transform(currentEvaluationToken, context.Scope);
                }

                return loopVariable.Value;
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
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
                    else
                    {
                        //throw Error.CreateExecutionErrorFrom(ExceptionCode.ExpectedLambdaResultToBeBooleanButFoundDifferentToken, matchResult);
                    }
                }
                finally
                {
                    context.Scope.RemoveCurrentVariablesLayer();
                }
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
