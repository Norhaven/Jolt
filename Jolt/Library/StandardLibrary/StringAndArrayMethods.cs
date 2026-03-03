using Jolt.Evaluation;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal static class StringAndArrayMethods
    {
        [JoltLibraryMethod("toUpperCase")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToUpperCase(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var upper = resolved switch
            {
                string text => text.ToUpper(),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().ToUpper(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to convert to upper case for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(upper);
        }

        [JoltLibraryMethod("toLowerCase")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ToLowerCase(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var upper = resolved switch
            {
                string text => text.ToLower(),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().ToLower(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to convert to lower case for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(upper);
        }

        [JoltLibraryMethod("trim")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Trim(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var trimmed = resolved switch
            {
                string text => text.Trim(),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().Trim(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to trim for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(trimmed);
        }

        [JoltLibraryMethod("trimStart")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? TrimStart(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var trimmed = resolved switch
            {
                string text => text.TrimStart(),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().TrimStart(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to trim start for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(trimmed);
        }

        [JoltLibraryMethod("trimEnd")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? TrimEnd(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var trimmed = resolved switch
            {
                string text => text.TrimEnd(),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().TrimEnd(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to trim end for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(trimmed);
        }

        [JoltLibraryMethod("replace")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Replace(object? value, object searchText, object replacementText, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var search = searchText is RangeVariable searchVariable && searchVariable.Value?.IsValue() == true ? searchVariable.Value.ToTypeOf<string>() : searchText?.ToString();
            var replacement = replacementText is RangeVariable replVariable && replVariable.Value?.IsValue() == true ? replVariable.Value.ToTypeOf<string>() : replacementText?.ToString();

            var replaced = resolved switch
            {
                string text => text.Replace(search, replacement),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().Replace(search, replacement),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to replace for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(replaced);
        }

        [JoltLibraryMethod("startsWith")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? StartsWith(object? value, object searchText, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var search = searchText is RangeVariable searchVariable && searchVariable.Value?.IsValue() == true ? searchVariable.Value.ToTypeOf<string>() : searchText?.ToString();

            var startsWith = resolved switch
            {
                string text => text.StartsWith(search),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().StartsWith(search),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check start for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(startsWith);
        }

        [JoltLibraryMethod("endsWith")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? EndsWith(object? value, object searchText, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var search = searchText is RangeVariable searchVariable && searchVariable.Value?.IsValue() == true ? searchVariable.Value.ToTypeOf<string>() : searchText?.ToString();

            var endsWith = resolved switch
            {
                string text => text.EndsWith(search),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().EndsWith(search),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to check end for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(endsWith);
        }

        [JoltLibraryMethod("reverse")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Reverse(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);
            var resultToken = context.CreateTokenFrom(resolved);

            var reversed = resultToken switch
            {
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => context.CreateTokenFrom(new string(jsonValue.ToTypeOf<string>().Reverse().ToArray())),
                IJsonArray array => context.CreateArrayFrom(array.Reverse().ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to reverse for unsupported object type '{value?.GetType()}'")
            };

            return reversed;
        }

        [JoltLibraryMethod("flatten")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Flatten(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);
            var resultToken = context.CreateTokenFrom(resolved);

            IEnumerable<IJsonToken> FlattenRecursive(IJsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is IJsonArray nestedArray)
                    {
                        foreach (var flatItem in FlattenRecursive(nestedArray))
                        {
                            yield return flatItem;
                        }
                    }
                    else
                    {
                        yield return item;
                    }
                }
            }

            var flattened = resultToken switch
            {
                IJsonArray array => context.CreateArrayFrom(FlattenRecursive(array).ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to flatten for unsupported object type '{value?.GetType()}'")
            };

            return flattened;
        }

        [JoltLibraryMethod("indexOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IndexOf(object? value, object searchText, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var search = searchText is RangeVariable searchVariable && searchVariable.Value?.IsValue() == true ? searchVariable.Value.ToTypeOf<string>() : searchText?.ToString();

            var index = resolved switch
            {
                string text => text.IndexOf(search),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().IndexOf(search),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get index of '{search}' for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(index);
        }

        [JoltLibraryMethod("substring")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Substring(object? value, Range range, EvaluationContext context)
        {
            static string AsString(IJsonValue token) => token.AsValue().ToTypeOf<string>();

            var resolved = context.ResolveValueOf<string>(value);

            var content = resolved switch
            {
                string text => text.Substring(range),
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => AsString(token).Substring(range),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get substring for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(content);
        }

        [JoltLibraryMethod("contains")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Contains(object? instance, object? value, EvaluationContext context)
        {
            var resolved = instance switch
            {
                RangeVariable variable when variable.Value?.IsString() == true => variable.Value?.ToTypeOf<string>(),
                RangeVariable variable => variable.Value?.ToTypeOf<IJsonArray>(),
                _ => context.ResolveQueryPathIfPresent(value)
            };

            var contains = resolved switch
            {
                string text when value is string valueText => text.Contains(valueText),
                IJsonArray array => array.Contains(context.CreateTokenFrom(value)),
                IJsonValue token when token.ValueType == JsonValueType.String => token.AsValue().ToTypeOf<string>().Contains(value?.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to determine contents for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(contains);
        }

        [JoltLibraryMethod("length")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Length(object? value, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<object>(value);
            var resultToken = context.CreateTokenFrom(resolved);

            var length = resultToken switch
            {
                IJsonArray array => array.Count(),
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => token.AsValue().ToTypeOf<string>().Length,
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to get length for unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(length);
        }

        [JoltLibraryMethod("slice")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Slice(object? value, Range range, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

            var content = resolved switch
            {
                object[] array => RuntimeHelpers.GetSubArray(array, range),
                IJsonArray array => RuntimeHelpers.GetSubArray(array.ToArray(), range),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to slice unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(content);
        }

        [JoltLibraryMethod("append")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Append(object? value, [VariadicEvaluation] object[]? additionalValues, EvaluationContext context)
        {
            var resolved = value switch
            {
                RangeVariable variable when variable.Value?.IsString() == true => variable.Value?.ToTypeOf<string>(),
                RangeVariable variable => variable.Value?.ToTypeOf<IJsonArray>(),
                _ => context.ResolveQueryPathIfPresent(value)
            };

            IJsonToken? resultToken = context.CreateTokenFrom(resolved);

            IJsonArray Concat(IJsonArray array, string value)
            {
                array.Add(context.CreateTokenFrom(value));
                return array;
            }

            foreach (var additionalValue in additionalValues ?? Enumerable.Empty<object>())
            {
                var resolvedValue = additionalValue is RangeVariable variable ? variable.Value?.ToTypeOf<object>() : context.ResolveQueryPathIfPresent(additionalValue);

                resultToken = (resultToken, resolvedValue) switch
                {
                    (IJsonArray first, IJsonArray second) => context.CreateArrayFrom(first.Concat(second).ToArray()),
                    (IJsonArray first, string second) => Concat(first, second),
                    (IJsonObject first, IJsonObject second) => context.CreateObjectFrom(first.Concat(second).ToArray()),
                    (IEnumerable<object> first, IEnumerable<object> second) => context.CreateTokenFrom(first.Concat(second).ToArray()),
                    (IJsonValue first, string second) when first.IsString() => context.CreateTokenFrom($"{first.ToTypeOf<string>()}{second}"),
                    (IJsonValue first, IJsonValue second) when first.IsString() && second.IsString() => context.CreateTokenFrom($"{first}{second}"),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to append with unsupported object types '{resultToken?.GetType()}' and '{resolvedValue?.GetType()}'")
                };
            }

            return resultToken;
        }

        [JoltLibraryMethod("joinWith")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? JoinWith(object? value, string delimiter, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<IJsonArray>(value);

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
            var resolved = context.ResolveValueOf<string>(value);

            var split = resolved switch
            {
                string text => text.Split(delimiter),
                IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => jsonValue.ToTypeOf<string>().Split(delimiter),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to split with unsupported object type '{value?.GetType()}'")
            };

            return context.CreateTokenFrom(split);
        }

        [JoltLibraryMethod("parseDateTime")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ParseDateTime(object? value, object? format, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);
            var formatStr = format is RangeVariable formatVariable && formatVariable.Value?.IsValue() == true ? formatVariable.Value.ToTypeOf<string>() : format?.ToString();

            try
            {
                var dateTime = resolved switch
                {
                    string text when formatStr != null => DateTime.ParseExact(text, formatStr, System.Globalization.CultureInfo.InvariantCulture),
                    IJsonValue jsonValue when formatStr != null && jsonValue.ValueType == JsonValueType.String => DateTime.ParseExact(jsonValue.ToTypeOf<string>(), formatStr, System.Globalization.CultureInfo.InvariantCulture),
                    string text => DateTime.Parse(text, System.Globalization.CultureInfo.InvariantCulture),
                    IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => DateTime.Parse(jsonValue.ToTypeOf<string>(), System.Globalization.CultureInfo.InvariantCulture),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to parse date time for unsupported object type '{value?.GetType()}'")
                };

                return context.CreateTokenFrom(dateTime.ToString("O"));
            }
            catch (FormatException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Unable to parse date time with format '{formatStr}': {ex.Message}");
            }
        }

        [JoltLibraryMethod("formatDateTime")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? FormatDateTime(object? value, object format, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);
            var formatStr = format is RangeVariable formatVariable && formatVariable.Value?.IsValue() == true ? formatVariable.Value.ToTypeOf<string>() : format?.ToString();

            try
            {
                var dateTime = resolved switch
                {
                    string text => DateTime.Parse(text),
                    IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => DateTime.Parse(jsonValue.ToTypeOf<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to format date time for unsupported object type '{value?.GetType()}'")
                };

                return context.CreateTokenFrom(dateTime.ToString(formatStr));
            }
            catch (FormatException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Unable to format date time with format '{formatStr}': {ex.Message}");
            }
        }

        [JoltLibraryMethod("currentDateTime")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? CurrentDateTime(EvaluationContext context)
        {
            return context.CreateTokenFrom(DateTime.Now.ToString("O"));
        }

        [JoltLibraryMethod("currentDateTimeUtc")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? CurrentDateTimeUtc(EvaluationContext context)
        {
            return context.CreateTokenFrom(DateTime.UtcNow.ToString("O"));
        }

        [JoltLibraryMethod("addDays")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? AddDays(object? value, object days, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);
            var daysValue = days is RangeVariable daysVariable && daysVariable.Value?.IsValue() == true ? daysVariable.Value.ToTypeOf<double>() : Convert.ToDouble(days);

            try
            {
                var dateTime = resolved switch
                {
                    string text => DateTime.Parse(text),
                    IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => DateTime.Parse(jsonValue.ToTypeOf<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to add days to unsupported object type '{value?.GetType()}'")
                };

                return context.CreateTokenFrom(dateTime.AddDays(daysValue).ToString("O"));
            }
            catch (OverflowException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Unable to add {daysValue} days to date time: {ex.Message}");
            }
        }

        [JoltLibraryMethod("addHours")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? AddHours(object? value, object hours, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);
            var hoursValue = hours is RangeVariable hoursVariable && hoursVariable.Value?.IsValue() == true ? hoursVariable.Value.ToTypeOf<double>() : Convert.ToDouble(hours);

            try
            {
                var dateTime = resolved switch
                {
                    string text => DateTime.Parse(text),
                    IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => DateTime.Parse(jsonValue.ToTypeOf<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to add hours to unsupported object type '{value?.GetType()}'")
                };

                return context.CreateTokenFrom(dateTime.AddHours(hoursValue).ToString("O"));
            }
            catch (OverflowException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Unable to add {hoursValue} hours to date time: {ex.Message}");
            }
        }

        [JoltLibraryMethod("addMinutes")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? AddMinutes(object? value, object minutes, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);
            var minutesValue = minutes is RangeVariable minutesVariable && minutesVariable.Value?.IsValue() == true ? minutesVariable.Value.ToTypeOf<double>() : Convert.ToDouble(minutes);

            try
            {
                var dateTime = resolved switch
                {
                    string text => DateTime.Parse(text),
                    IJsonValue jsonValue when jsonValue.ValueType == JsonValueType.String => DateTime.Parse(jsonValue.ToTypeOf<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to add minutes to unsupported object type '{value?.GetType()}'")
                };

                return context.CreateTokenFrom(dateTime.AddMinutes(minutesValue).ToString("O"));
            }
            catch (OverflowException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Unable to add {minutesValue} minutes to date time: {ex.Message}");
            }
        }
    }
}
