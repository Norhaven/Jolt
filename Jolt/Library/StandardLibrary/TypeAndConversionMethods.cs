using Jolt.Evaluation;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal static class TypeAndConversionMethods
    {
        [JoltLibraryMethod("isMissing")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsMissing(object? pathOrValue, EvaluationContext context)
        {
            var valueExists = pathOrValue != null;

            if (pathOrValue is DereferencedPath dereferenced)
            {
                return context.CreateTokenFrom(dereferenced.MissingPaths.Length > 0);
            }

            if (pathOrValue is string path && context.JsonContext.QueryPathProvider.IsQueryPath(path))
            {
                var tokenValue = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot);
                var tokenMissing = tokenValue == null;

                return context.CreateTokenFrom(tokenMissing);
            }

            return context.CreateTokenFrom(!valueExists);
        }

        [JoltLibraryMethod("isNull")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsNull(object? pathOrValue, EvaluationContext context)
        {
            var valueExists = pathOrValue != null;

            if (pathOrValue is DereferencedPath dereferenced)
            {
                return context.CreateTokenFrom(dereferenced.MissingPaths.Length == 0 && dereferenced.ObtainableToken.AsValue().ValueType == JsonValueType.Null);
            }

            if (pathOrValue is string path && context.JsonContext.QueryPathProvider.IsQueryPath(path))
            {
                var tokenValue = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot);
                var tokenNull = tokenValue != null && tokenValue.Type == JsonTokenType.Value && tokenValue.AsValue().ValueType == JsonValueType.Null;

                return context.CreateTokenFrom(tokenNull);
            }

            return context.CreateTokenFrom(!valueExists);
        }

        [JoltLibraryMethod("exists")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Exists(object? pathOrValue, EvaluationContext context)
        {
            var valueExists = pathOrValue != null;

            if (pathOrValue is DereferencedPath dereferenced)
            {
                if (dereferenced.MissingPaths.Length > 0)
                {
                    return context.CreateTokenFrom(false);
                }

                pathOrValue = dereferenced.ObtainableToken;
            }

            if (pathOrValue is RangeVariable variable)
            {
                var variableValue = variable.Value;
                var tokenExists = variableValue != null && variableValue.Type != JsonTokenType.Null && variableValue.ToTypeOf<object>() != null;

                return context.CreateTokenFrom(tokenExists);
            }

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

        [JoltLibraryMethod("roundTo")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? RoundTo(object? value, object? decimalPlaces, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            var resolvedValue = context.CreateTokenFrom(resolved);

            object? rounded = resolvedValue switch
            {
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is long i => Math.Round(token.ToTypeOf<double>(), (int)i),
                IJsonValue token when token.ValueType == JsonValueType.Number && decimalPlaces is IJsonValue val && val.ValueType == JsonValueType.Number => Math.Round(token.ToTypeOf<double>(), val.ToTypeOf<int>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine rounding for unsupported object types '{value?.GetType()}' and '{decimalPlaces?.GetType()}'")
            };

            return context.CreateTokenFrom(rounded);
        }

        [JoltLibraryMethod("isInteger")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsInteger(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return context.CreateTokenFrom(resolved is int || resolved is long || ((resolved is double || resolved is decimal) && !resolved.ToString().Contains(".")));
        }

        [JoltLibraryMethod("isString")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsString(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return context.CreateTokenFrom(resolved is string);
        }

        [JoltLibraryMethod("isDecimal")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsDecimal(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return context.CreateTokenFrom(resolved is decimal || resolved is double);
        }

        [JoltLibraryMethod("isBoolean")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsBoolean(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return context.CreateTokenFrom(resolved is bool);
        }

        [JoltLibraryMethod("isArray")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsArray(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

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

            var resolved = value switch
            {
                RangeVariable variable when variable.Value?.Type == JsonTokenType.Array => variable.Value.ToTypeOf<IJsonArray>(),
                RangeVariable variable => variable.Value?.ToTypeOf<string>(),
                _ => context.ResolveQueryPathIfPresent(value)
            };

            var empty = resolved switch
            {
                IJsonArray array => array.Length == 0,
                IJsonValue val when val.IsString() => val.ToTypeOf<string>().Length == 0,
                string val => val.Length == 0 || (val.Length == 2 && val.Replace("'", string.Empty).Length == 0), // The presence of a string literal should register as empty.
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check emptiness with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(empty);
        }

        [JoltLibraryMethod("toInteger")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToInteger(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return ConvertToType<long>(resolved, context);
        }

        [JoltLibraryMethod("toString")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToString(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            var convertedValue = resolved?.ToString();

            return context.CreateTokenFrom(convertedValue);
        }

        [JoltLibraryMethod("toDecimal")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToDecimal(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return ConvertToType<double>(resolved, context);
        }

        [JoltLibraryMethod("toBoolean")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToBool(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);

            return ConvertToType<bool>(resolved, context);
        }

        private static IJsonToken? ConvertToType<T>(object? value, EvaluationContext context)
        {
            if (value is IJsonToken token)
            {
                value = token.AsValue().ToTypeOf<object>();
            }
            else if (value is RangeVariable val)
            {
                value = val.Value.Type switch
                {
                    JsonTokenType.Value when val.Value.AsValue().ValueType == JsonValueType.String => val.Value.AsValue().ToTypeOf<string>(),
                    JsonTokenType.Value when val.Value.AsValue().ValueType == JsonValueType.Number => val.Value.AsValue().ToTypeOf<double>(),
                    JsonTokenType.Value when val.Value.AsValue().ValueType == JsonValueType.Boolean => val.Value.AsValue().ToTypeOf<bool>(),
                    _ => val.Value
                };
            }

            if (value is null || value is T)
            {
                return context.CreateTokenFrom(value);
            }
            else if (value is string text)
            {
                if (typeof(T) == typeof(long) && long.TryParse(text, out var longResult))
                {
                    return context.CreateTokenFrom(longResult);
                }
                else if (typeof(T) == typeof(double) && double.TryParse(text, out var doubleResult))
                {
                    return context.CreateTokenFrom(doubleResult);
                }
                else if (typeof(T) == typeof(bool) && bool.TryParse(text, out var boolResult))
                {
                    return context.CreateTokenFrom(boolResult);
                }
            }

            var convertedValue = Convert.ChangeType(value, typeof(T));

            return context.CreateTokenFrom(convertedValue);
        }
    }
}
