using FluentAssertions;
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
        obj.Should().NotBeNull("because the object exists in the source document");

        foreach(var value in values)
        {
            obj.PropertyValueFor<T>(value.PropertyName).Should().Be(value.ExpectedValue);
        }
    }

    public static void ShouldContainProperties<T>(this IJsonArray array, params (int Index, string PropertyName, T ExpectedValue)[] values)
    {
        array.Should().NotBeNull("because the array exist in the source document");
        array.Length.Should().Be(values.Length, $"because there are {values.Length} elements in each array");

        foreach (var value in values)
        {
            array.ArrayPropertyValueFor<T>(value.Index, value.PropertyName).Should().Be(value.ExpectedValue);
        }
    }

    public static void ShouldContain<T>(this IJsonArray array, params T[] values)
    {
        array.Should().NotBeNull("because both arrays exist in the source document");
        array.Length.Should().Be(values.Length, $"because there are {values.Length} elements in each array");
        array.As<T>().Should().BeEquivalentTo(values.AsEnumerable());
    }
}
