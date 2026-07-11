using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure.Streaming
{
    public sealed class StreamingOptions
    {
        public StreamingOutputFormat OutputFormat { get; }
        public StreamingDelimiter Delimiter { get; }
        public bool IsAsync { get; }

        public StreamingOptions(StreamingOutputFormat outputFormat = StreamingOutputFormat.Default, StreamingDelimiter delimiter = StreamingDelimiter.Default, bool isAsync = true)
        {
            OutputFormat = outputFormat;
            Delimiter = delimiter;
            IsAsync = isAsync;
        }
    }
}
