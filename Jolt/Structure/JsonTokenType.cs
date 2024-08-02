using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents the overall type of a general JSON structure.
    /// </summary>
    public enum JsonTokenType
    {
        Unknown = 0,
        Object,
        Array,
        Property,
        Value,
        Null
    }
}
