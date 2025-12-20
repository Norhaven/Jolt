using Jolt.Json.Tests.Resources.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Tests.Resources.Extensions
{
    public static class JsonExtensions
    {
        public static T PropertyValueFor<T>(this IJsonObject json, string propertyName)
        {
            object underlyingObject = (IJsonValue)json?[propertyName] switch
            {
                null => default,
                var x when x.ValueType == JsonValueType.String => x.ToTypeOf<string>(),
                var x when x.ValueType == JsonValueType.Number => x.ToTypeOf<double>(),
                var x when x.ValueType == JsonValueType.Boolean => x.ToTypeOf<bool>(),
                var x when x.ValueType == JsonValueType.Null => default,
                _ => throw new ArgumentOutOfRangeException($"Expected a value for property name '{propertyName}' in order to verify the test results but received an unsupported type")
            };

            return (T)Convert.ChangeType(underlyingObject, typeof(T));
        }

        public static T ArrayPropertyValueFor<T>(this IJsonArray array, int index, string propertyName)
        {
            return array[index].AsObject().PropertyValueFor<T>(propertyName);
        }

        public static IEnumerable<T> As<T>(this IJsonArray array) => array.Select(x => x.AsValue().ToTypeOf<T>());

        public static void ShouldContainProperties<T>(this IJsonObject obj, params (string PropertyName, T ExpectedValue)[] values)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "The object to verify cannot be null because the object should exist in the source document");
            }

            foreach (var (propertyName, expectedValue) in values)
            {
                var propertyValue = obj.PropertyValueFor<T>(propertyName);

                if (!EqualityComparer<T>.Default.Equals(propertyValue, expectedValue))
                {
                    throw new TestExpectationFailedException($"Property '{propertyName}' was expected to have the value '{expectedValue}' but had '{propertyValue}' instead");
                }
            }
        }

        public static void ShouldContainProperties<T>(this IJsonArray array, params (int Index, string PropertyName, T ExpectedValue)[] values)
        {
            if (array == null)
            {
                throw new ArgumentNullException("The array to verify cannot be null because the array exists in the source document");
            }

            if (array.Length != values.Length)
            {
                throw new TestExpectationFailedException($"The array was expected to have {values.Length} elements but had {array.Length} instead");
            }

            foreach (var value in values)
            {
                var propertyValue = array.ArrayPropertyValueFor<T>(value.Index, value.PropertyName);
                
                if (!EqualityComparer<T>.Default.Equals(propertyValue, value.ExpectedValue))
                {
                    throw new TestExpectationFailedException($"Array property '{value.PropertyName}' at index {value.Index} was expected to have the value '{value.ExpectedValue}' but had '{propertyValue}' instead");
                }
            }
        }

        public static void ShouldContain<T>(this IJsonArray array, params T[] values)
        {
            if (array == null)
            {
                throw new ArgumentNullException("The array to verify cannot be null because the array exists in the source document");
            }

            if (values == null)
            {
                throw new ArgumentNullException("The values array to verify cannot be null because the array exists in the source document");
            }

            if (array.Length != values.Length)
            {
                throw new TestExpectationFailedException($"The array was expected to have {values.Length} elements but had {array.Length} instead");
            }

            var typedArray = array.As<T>().ToArray();

            for (var i = 0; i < typedArray.Length; i++)
            {
                var element = typedArray[i];

                if (element is null)
                {
                    throw new TestExpectationFailedException("Array element was expected to be non-null");
                }

                var value = values[i];
                var elementType = element.GetType();
                var valueType = value.GetType();

                if (elementType.FullName != valueType.FullName)
                {
                    throw new TestExpectationFailedException($"Array element of type '{elementType.FullName}' at index {i} is not the same type as value of type '{valueType.FullName}'");
                }

                if (elementType.IsPrimitive || elementType == typeof(string))
                {
                    if (!EqualityComparer<T>.Default.Equals(element, value))
                    {
                        throw new TestExpectationFailedException($"Array element at index {i} was expected to have the value '{value}' but had '{element}' instead");
                    }

                    continue;
                }

                var properties = element?.GetType().GetProperties();

                foreach (var property in properties)
                {
                    if (property.GetValue(element) != property.GetValue(value))
                    {
                        throw new TestExpectationFailedException($"Array element property '{property.Name}' at index {i} was expected to have the value '{property.GetValue(value)}' but had '{property.GetValue(element)}' instead");
                    }
                }
            }
        }
    }
}
