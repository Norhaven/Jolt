using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    /// <summary>
    /// Sets a null JSON property value in the source document by name (if applicable).
    /// </summary>
    public sealed class SourceHasNoValueAttribute : SourceHasAttribute
    {
        public SourceHasNoValueAttribute() : base(SourceValueType.Unknown, Default.Value, default)
        {
        }
    }
}
