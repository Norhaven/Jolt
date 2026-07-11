using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure.Streaming
{
    public enum StreamingDelimiter
    {
        Default = 0,
        None = 1,
        NewLine = 2,
        Comma = 3,
        Rfc7464 = 4,
    }
}
