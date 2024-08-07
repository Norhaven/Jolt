using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasIntegerAttribute : SourceHasAttribute
{
    public SourceHasIntegerAttribute(string name, object? value) : base(SourceValueType.IntegerLiteral, name, value)
    {
    }
}
