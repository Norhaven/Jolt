using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure.Streaming
{
    public enum StreamingOutputFormat
    {
        Default = 0,
        SingleLinePerObject = 1,
        MultiLinePerObject = 2,
        ArrayWithSingleLinePerObject = 3,
        ArrayWithMultiLinePerObject = 4
    }
}
