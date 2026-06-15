using Jolt.Json.Tests.Resources.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Testing.Resources.Extensions
{
    public static class JsonExtensions
    {
        public static T PropertyValueFor<T>(this IJsonObject json, string propertyName)
        {
            object underlyingObject;
            var jsonValue = (IJsonValue)json?[propertyName];

            if (jsonValue == null)
            {
                underlyingObject = default;
            }
            else
            {
                switch (jsonValue.ValueType)
                {
                    case JsonValueType.String:
                        underlyingObject = jsonValue.ToTypeOf<string>();
                        break;
                    case JsonValueType.Number:
                        underlyingObject = jsonValue.ToTypeOf<double>();
                        break;
                    case JsonValueType.Boolean:
                        underlyingObject = jsonValue.ToTypeOf<bool>();
                        break;
                    case JsonValueType.Null:
                        underlyingObject = default;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Expected a value for property name '{propertyName}' in order to verify the test results but received an unsupported type");
                }
            }

            return (T)Convert.ChangeType(underlyingObject, typeof(T));
        }

        public static T ArrayPropertyValueFor<T>(this IJsonArray array, int index, string propertyName)
        {
            return array[index].AsObject().PropertyValueFor<T>(propertyName);
        }

        public static IEnumerable<T> As<T>(this IJsonArray array) => array.Select(x => x.AsValue().ToTypeOf<T>());
        
        
    }
}
