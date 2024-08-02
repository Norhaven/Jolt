using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Extensions;

internal static class JsonExtensions
{
    public static T PropertyValueFor<T>(this IJsonObject json, string propertyName)
    {
        object? underlyingObject = (IJsonValue)json?[propertyName] switch
        {
            null => default,
            var x when x.ValueType == JsonValueType.String => x.ToTypeOf<string>(),
            var x when x.ValueType == JsonValueType.Number => x.ToTypeOf<decimal>(),
            var x when x.ValueType == JsonValueType.Boolean => x.ToTypeOf<bool>(),
            var x when x.ValueType == JsonValueType.Null => default,
            _ => throw new ArgumentOutOfRangeException($"Expected a value for property name '{propertyName}' in order to verify the test results but received an unsupported type")
        };

        return (T)Convert.ChangeType(underlyingObject, typeof(T));
    }
}
