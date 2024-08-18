using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

[AttributeUsage(AttributeTargets.Method)]
internal class SourcePropertyIsAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
