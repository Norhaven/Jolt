using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents the type of a JSON value.
    /// </summary>
    public enum JsonValueType
    {
        Unknown = 0,
        String = 1,
        Number = 2,
        Boolean = 3,
        Null = 4
    }
}
