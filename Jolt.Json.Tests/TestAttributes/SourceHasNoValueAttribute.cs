using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class SourceHasNoValueAttribute : SourceHasAttribute
{
    public SourceHasNoValueAttribute() : base(SourceValueType.Unknown, Default.Value, default)
    {
    }
}
