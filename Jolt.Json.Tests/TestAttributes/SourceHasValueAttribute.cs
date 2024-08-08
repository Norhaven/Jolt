using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasValueAttribute : SourceHasAttribute
{
    public SourceHasValueAttribute(object? value) : base(SourceValueType.Unknown, Default.Value, value)
    {
    }
}
