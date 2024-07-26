using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
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
