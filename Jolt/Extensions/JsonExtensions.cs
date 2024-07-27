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
        public static bool AllElementsAreOfType<T, T1, T2>(this IJsonArray array) => array.AllElementsAreOfTypes(typeof(T), typeof(T1), typeof(T2));

        public static bool IsOnlyIntegers(this IJsonArray array) => array.AllElementsAreOfType<int, long>();
        public static bool IsOnlyNumericPrimitives(this IJsonArray array) => array.AllElementsAreOfType<int, long, double>();

        public static IEnumerable<double> AsDoubles(this IJsonArray array) => array.Select(x => x.ToTypeOf<double>());
        public static IEnumerable<long> AsInt64s(this IJsonArray array) => array.Select(x => x.ToTypeOf<long>());

        public static bool IsValue(this IJsonToken token) => token.Type == JsonTokenType.Value;
        public static bool IsInteger(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<int>();
        public static bool IsDouble(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<double>();
        public static bool IsTypeOf<T>(this IJsonValue value) => value.IsObject<T>();

        public static IEnumerable<T> AsSequenceOf<T>(this object? value)
        {
            return value switch
            {
                IEnumerable<int> integers => integers.Cast<T>(),
                IEnumerable<double> doubles => doubles.Cast<T>(),
                IJsonArray array => array.Select(x => x.AsValue().ToTypeOf<T>()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to convert to sequence for unsupported object type '{value?.GetType()}'")
            };
        }
    }
}
