using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasBooleanAttribute : SourceHasAttribute
{
    public SourceHasBooleanAttribute(string name, object? value) : base(SourceValueType.BooleanLiteral, name, value)
    {
    }
}
