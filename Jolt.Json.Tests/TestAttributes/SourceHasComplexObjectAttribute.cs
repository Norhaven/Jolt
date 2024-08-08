using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasComplexObjectAttribute : SourceHasAttribute
{
    public SourceHasComplexObjectAttribute(object? value) : base(SourceValueType.Object, Default.Value, value)
    {
    }

    public SourceHasComplexObjectAttribute(string name, object? value) : base(SourceValueType.Object, name, value)
    {
    }
}
