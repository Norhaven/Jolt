using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Structure.Streaming.Transformations
{
    internal sealed class StreamingLinesTransformation<TContext> : StreamingTransformation<TContext> where TContext : IJsonContext
    {
        public StreamingLinesTransformation(JoltTransformer<TContext> transformer, TextReader reader, TextWriter writer, StreamingOptions options) 
            : base(transformer, reader, writer, options)
        {
        }

        protected override Task<string?> ReadEntryAsync()
        {
            return _reader.ReadLineAsync();
        }

        protected override string? ReadEntry()
        {
            return _reader.ReadLine();
        }

        protected override Task WriteAsync(string text)
        {
            return _writer.WriteLineAsync(text);
        }

        protected override void Write(string text)
        {
            _writer.WriteLine(text);
        }
    }
}
