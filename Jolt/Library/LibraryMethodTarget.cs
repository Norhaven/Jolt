using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    [Flags]
    internal enum LibraryMethodTarget
    {
        None = 0,
        PropertyName = 1 << 0,
        PropertyValue = 1 << 1
    }
}
