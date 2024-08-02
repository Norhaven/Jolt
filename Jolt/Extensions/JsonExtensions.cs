using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Extensions
{
    internal static class JsonExtensions
    {
        public static bool AllElementsAreOfTypes(this IJsonArray array, params Type[] types) => array.All(x => x.IsValue() && x.ToTypeOf<object>().GetType().IsAnyOf(types));
        public static bool AllElementsAreOfType<T>(this IJsonArray array) => array.Length > 0 && array.All(x => x.IsValue() && x.AsValue().IsTypeOf<T>());
        public static bool AllElementsAreOfType<T, T1>(this IJsonArray array) => array.AllElementsAreOfTypes(typeof(T), typeof(T1));
        public static bool AllElementsAreOfType<T, T1, T2, T3>(this IJsonArray array) => array.AllElementsAreOfTypes(typeof(T), typeof(T1), typeof(T2), typeof(T3));

        public static bool IsOnlyIntegers(this IJsonArray array) => array.AllElementsAreOfType<int, long>();
        public static bool IsOnlyNumericPrimitives(this IJsonArray array) => array.AllElementsAreOfType<int, long, decimal, double>();

        public static bool IsValue(this IJsonToken token) => token.Type == JsonTokenType.Value;
        public static bool IsString(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<string>();
        public static bool IsBoolean(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<bool>();
        public static bool IsTypeOf<T>(this IJsonValue value) => value.IsObject<T>();

        public static IEnumerable<T> AsSequenceOf<T>(this object? value)
        {
            return value switch
            {
                IEnumerable<int> integers => integers.Cast<T>(),
                IEnumerable<decimal> decimals => decimals.Cast<T>(),
                IEnumerable<double> doubles => doubles.Cast<T>(),
                IJsonArray array => array.Select(x => x.AsValue().ToTypeOf<T>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to convert to sequence for unsupported object type '{value?.GetType()}'")
            };
        }
    }
}
