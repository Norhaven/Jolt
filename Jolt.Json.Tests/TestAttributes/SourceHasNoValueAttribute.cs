using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Sets a null JSON property value in the source document by name (if applicable).
/// </summary>
internal class SourceHasNoValueAttribute() : SourceHasAttribute(SourceValueType.Unknown, Default.Value, default)
{
}
