using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Sets a complex object JSON property value in the source document by name (if applicable).
/// </summary>
internal class SourceHasComplexObjectAttribute : SourceHasAttribute
{
    public SourceHasComplexObjectAttribute(object? value) : base(SourceValueType.Object, Default.Value, value)
    {
    }

    public SourceHasComplexObjectAttribute(string name, object? value) : base(SourceValueType.Object, name, value)
    {
    }
}
