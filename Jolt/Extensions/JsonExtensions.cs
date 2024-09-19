using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Extensions
{
    internal static class JsonExtensions
    {
        public static bool IsValue(this IJsonToken token) => token.Type == JsonTokenType.Value;
        public static bool IsString(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<string>();
        public static bool IsBoolean(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<bool>();
        public static bool IsTypeOf<T>(this IJsonValue value) => value.IsObject<T>();
        public static bool IsGroup(this IJsonArray array) => array.All(x => x.AsObject().HasProperty("key") && x.AsObject().HasProperty("results"));

        public static bool ContainsOnly(this IJsonArray array, JsonArrayElementType elementType)
        {
            var allFlags = Enum.GetValues(typeof(JsonArrayElementType)).Cast<JsonArrayElementType>();

            foreach(var flag in allFlags)
            {
                if (array.ElementTypes.HasFlag(flag) && !elementType.HasFlag(flag))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ContainsOnlyStrings(this IJsonArray array) => array.ContainsOnly(JsonArrayElementType.String);
        public static bool ContainsOnlyIntegers(this IJsonArray array) => array.ContainsOnly(JsonArrayElementType.Integer);
        public static bool ContainsOnlyNumbers(this IJsonArray array) => array.ContainsOnly(JsonArrayElementType.Integer | JsonArrayElementType.Decimal);
        public static bool ContainsAtLeastOneDecimal(this IJsonArray array) => array.ElementTypes.HasFlag(JsonArrayElementType.Decimal);

        public static IEnumerable<T> AsSequenceOf<T>(this object? value)
        {
            return value switch
            {
                IEnumerable<int> integers => integers.Cast<T>(),
                IEnumerable<decimal> decimals => decimals.Cast<T>(),
                IEnumerable<double> doubles => doubles.Cast<T>(),
                IJsonArray array => array.Select(x => (T)Convert.ChangeType(x.AsValue().ToTypeOf<object>(), typeof(T))),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to convert to sequence for unsupported object type '{value?.GetType()}'")
            };
        }        
    }
}
