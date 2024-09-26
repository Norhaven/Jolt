using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Creates the expectation that a specific value will be returned as the result in the specified property (if applicable).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class ExpectsResultAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the property that should contain the expected value.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the expected value.
    /// </summary>
    public object? Value { get; }

    public ExpectsResultAttribute(object? value)
    {
        PropertyName = Default.Result;
        Value = value;
    }

    public ExpectsResultAttribute(string propertyName, object? value)
    {
        PropertyName = propertyName;
        Value = value;
    }
}
