using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Specifies the category of a message, indicating its origin or processing stage. 
    /// </summary>
    /// <remarks>Use this enumeration to distinguish between messages generated during different phases, such
    /// as parsing, execution, or resolution. This can be useful for filtering, logging, or handling messages based on
    /// their context.</remarks>
    public enum MessageCategory
    {
        Unknown = 0,
        Parsing,
        Execution,
        Resolution
    }
}
