using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a methodology for querying JSON.
    /// </summary>
    public enum JsonQueryMode
    {
        Unknown = 0,
        StartFromRoot = 1,
        StartFromClosestMatch = 2
    }
}
