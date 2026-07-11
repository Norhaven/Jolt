using Jolt.Structure.Streaming.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Structure.Streaming.Transformations
{
    internal class StreamingRfc7464SequenceTransformation<TContext> : StreamingTransformation<TContext> where TContext : IJsonContext
    {
        public StreamingRfc7464SequenceTransformation(JoltTransformer<TContext> transformer, TextReader reader, TextWriter writer, StreamingOptions options) 
            : base(transformer, reader, writer, options)
        {
        }

        protected override string GetDelimiter()
        {
            return _options.GetDelimiterOrDefault((0x1E).ToString());
        }
    }
}
