using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasStringAttribute : SourceHasAttribute
{
    public SourceHasStringAttribute(string name, object? value) : base(SourceValueType.StringLiteral, name, value)
    {
    }
}
