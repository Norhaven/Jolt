using Jolt.Json.Tests.Resources.Exceptions;
using Jolt.Structure;
using Jolt.Testing.Assertions.Exceptions;
using Jolt.Testing.Resources.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Testing.Assertions
{
    internal static class JsonAssertions
    {
        public static void ExpectsNull(this object obj, string because)
        {
            if (obj != null)
            {
                throw new NullExpectationFailedException(because);
            }
        }

        public static void ExpectsNonNull(this object obj, string because)
        {
            if (obj == null)
            {
                throw new NonNullExpectationFailedException(because);
            }
        }

        public static void ExpectsEqualTo<T>(this T actual, T expected, string because)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new EqualityExpectationFailureException(expected, because);
            }
        }

        public static void ExpectsContentsEqualTo<T>(this IEnumerable<T> actual, IEnumerable<T> expected, string because)
        {
            var actualList = actual.ToList();
            var expectedList = expected.ToList();

            if (actualList.Count != expectedList.Count)
            {
                throw new EqualityExpectationFailureException(expectedList, because);
            }

            for (var i = 0; i < actualList.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(actualList[i], expectedList[i]))
                {
                    throw new EqualityExpectationFailureException(expectedList, because);
                }
            }
        }

        public static void ExpectsPropertyEqualTo<T>(this IJsonObject obj, string propertyName, T expected, string because)
        {
            var actualProperty = obj[propertyName].ToTypeOf<T>();

            if (!EqualityComparer<T>.Default.Equals(expected, actualProperty))
            {
                throw new EqualityExpectationFailureException(expected, $"{because} for property '{propertyName}'");
            }
        }

        public static void ExpectsPropertyNonNull<T>(this IJsonObject obj, string propertyName, string because) where T : class
        {
            var actualProperty = obj[propertyName]?.ToTypeOf<T>();

            if (actualProperty == null)
            {
                throw new NonNullExpectationFailedException($"{because} for property '{propertyName}'");
            }
        }

        public static void ExpectsPropertyNull<T>(this IJsonObject obj, string propertyName, string because) where T : class
        {
            var actualProperty = obj[propertyName]?.ToTypeOf<T>();

            if (actualProperty != null)
            {
                throw new NullExpectationFailedException($"{because} for property '{propertyName}'");
            }
        }

        public static void ExpectsArrayContains<T>(this IJsonObject obj, string propertyName, params T[] values)
        {
            values = values ?? Array.Empty<T>();

            var array = obj[propertyName].AsArray();

            array.Length.ExpectsEqualTo(values.Length, "because the array should contain the same number of elements as the provided values");

            for (var i = 0; i < array.Length; i++)
            {
                array[i].ToTypeOf<T>().ExpectsEqualTo(values[i], $"because that is the value in the source document at index '{i}'");
            }
        }

        public static void ExpectsArrayNonNullAndNonEmpty(this IJsonArray array, string because)
        {
            array.ExpectsNonNull(because);
            array.Length.ExpectsGreaterThan(0, $"because the array should contain at least one element");
        }

        public static void ExpectsGreaterThan(this int actual, int threshold, string because)
        {
            if (actual <= threshold)
            {
                throw new GreaterThanExpectationFailureException(threshold, because);
            }
        }

        public static void ExpectsContainsProperties<T>(this IJsonObject obj, params (string PropertyName, T ExpectedValue)[] values)
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

        public static void ExpectsContainsProperties<T>(this IJsonArray array, params (int Index, string PropertyName, T ExpectedValue)[] values)
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

        public static void ExpectContainsProperties<T>(this IJsonArray array, params (int Index, string PropertyName, T ExpectedValue)[] values)
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

        public static void ExpectsContains<T>(this IJsonArray array, params T[] values)
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

                if (element == null)
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
