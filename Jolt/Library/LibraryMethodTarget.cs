using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents the allowed locations that a library method may be used within.
    /// </summary>
    [Flags]
    internal enum LibraryMethodTarget
    {
        None = 0,
        PropertyName = 1 << 0,
        PropertyValue = 1 << 1,
        StatementBlock = 1 << 2,
    }
}
