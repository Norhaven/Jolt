using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasDecimalAttribute : SourceHasAttribute
{
    public SourceHasDecimalAttribute(string name, object? value) : base(SourceValueType.DecimalLiteral, name, value)
    {
    }
}
