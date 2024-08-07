using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasAttribute : Attribute
{
    public string Name { get; }
    public SourceValueType Type { get; }
    public object? Value { get; }

    public SourceHasAttribute(SourceValueType type, string name, object? value)
    {
        Name = name;
        Type = type;
        Value = value;
    }
}
