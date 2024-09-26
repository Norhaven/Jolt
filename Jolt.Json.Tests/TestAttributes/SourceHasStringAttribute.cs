using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Sets a string JSON property value in the source document by name (if applicable).
/// </summary>
internal class SourceHasStringAttribute : SourceHasAttribute
{
    public SourceHasStringAttribute(object? value) : base(SourceValueType.StringLiteral, Default.Value, value)
    { 
    }

    public SourceHasStringAttribute(string name, object? value) : base(SourceValueType.StringLiteral, name, value)
    {
    }
}
