using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    /// <summary>
    /// Represents the type of method call needed to invoke a specific method.
    /// </summary>
    public enum CallType
    {
        Unknown = 0,
        Static = 1,
        Instance = 2
    }
}
