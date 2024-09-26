using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Sets a decimal JSON property value in the source document by name (if applicable).
/// </summary>
internal class SourceHasDecimalAttribute : SourceHasAttribute
{
    public SourceHasDecimalAttribute(object? value) : base(SourceValueType.DecimalLiteral, Default.Value, value)
    {
    }

    public SourceHasDecimalAttribute(string name, object? value) : base(SourceValueType.DecimalLiteral, name, value)
    {
    }
}
