using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourcePropertyIsAttribute : Attribute
{
    public string Name { get; }

    public SourcePropertyIsAttribute(string name)
    {
        Name = name;
    }
}
