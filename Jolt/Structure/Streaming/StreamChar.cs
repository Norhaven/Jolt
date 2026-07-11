using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure.Streaming
{
    internal static class StreamChar
    {
        public const int EndOfStream = -1;
        public const int Rfc7463RecordSeparator = 0x1E;
        public const char WhiteSpace = ' ';
        public const char Tab = '\t';
        public const char CarriageReturn = '\r';
        public const char NewLine = '\n';
        public const char ArrayStart = '[';
        public const char ArrayEnd = ']';
        public const char ObjectStart = '{';
        public const char ObjectEnd = '}';
        public const char Comma = ',';
    }
}
