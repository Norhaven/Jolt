using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure.Streaming
{
    public sealed class StreamingOptions
    {
        public StreamingOutputFormat OutputFormat { get; }
        public StreamingDelimiter Delimiter { get; }

        public StreamingOptions(StreamingOutputFormat outputFormat = StreamingOutputFormat.Default, StreamingDelimiter delimiter = StreamingDelimiter.Default)
        {
            OutputFormat = outputFormat;
            Delimiter = delimiter;
        }
    }
}
