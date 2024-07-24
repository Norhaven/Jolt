using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Extensions
{
    public static class JsonExtensions
    {
        public static bool AllElementsAreOfTypes(this IJsonArray array, params Type[] types) => array.All(x => x.IsValue() && x.ToTypeOf<object>().GetType().IsAnyOf(types));
        public static bool AllElementsAreOfType<T>(this IJsonArray array) => array.Length > 0 && array.All(x => x.IsValue() && x.AsValue().IsTypeOf<T>());

        public static bool IsValue(this IJsonToken token) => token.Type == JsonTokenType.Value;
        public static bool IsInteger(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<int>();
        public static bool IsDouble(this IJsonToken token) => token.IsValue() && token.AsValue().IsTypeOf<double>();
        public static bool IsTypeOf<T>(this IJsonValue value) => value.IsObject<T>();
    }
}
