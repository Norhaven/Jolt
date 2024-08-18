using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

[AttributeUsage(AttributeTargets.Method)]
internal class SourceHasAttribute(SourceValueType type, string name, object? value) : Attribute
{
    public string Name { get; } = name;
    public SourceValueType Type { get; } = type;
    public object? Value { get; } = value;
}
