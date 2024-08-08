using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class ExpectsResultAttribute : Attribute
{
    public string PropertyName { get; }
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
