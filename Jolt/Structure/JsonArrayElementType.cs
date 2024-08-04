using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    [Flags]
    public enum JsonArrayElementType
    {
        None = 0,
        Array = 1 << 0,
        Object = 1 << 1,
        String = 1 << 2,
        Decimal = 1 << 3,
        Integer = 1 << 4,
        Boolean = 1 << 5,
        Null = 1 << 6
    }
}
