using Jolt.Structure.Streaming.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jolt.Structure.Streaming.Transformations
{
    internal abstract class StreamingTransformation<TContext> where TContext : IJsonContext
    {
        protected readonly Dictionary<StreamingDelimiter, string> _delimiterChars = new Dictionary<StreamingDelimiter, string>
        {
            [StreamingDelimiter.None] = string.Empty,
            [StreamingDelimiter.NewLine] = "\n",
            [StreamingDelimiter.Comma] = ",",
            [StreamingDelimiter.Rfc7464] = (0x1E).ToString()
        };

        protected readonly JoltTransformer<TContext> _transformer;
        protected readonly TextReader _reader;
        protected readonly TextWriter _writer;
        protected readonly StreamingOptions _options;

        private int _entryHandledCount = 0;
        private int _currentDepth = 0;

        public int EntryHandledCount => _entryHandledCount;

        public StreamingTransformation(JoltTransformer<TContext> transformer, TextReader reader, TextWriter writer, StreamingOptions options)
        {
            _transformer = transformer;
            _reader = reader;
            _writer = writer;
            _options = options;
        }

        protected virtual async Task WriteAsync(string text) => await _writer.WriteAsync(text);
        protected virtual void Write(string text) => _writer.Write(text);

        protected virtual async Task<string?> ReadEntryAsync()
        {
            var buffer = new char[1];

            await _reader.ReadAsync(buffer, 0, 1).ConfigureAwait(continueOnCapturedContext: false);

            if (buffer[0] == 0)
            {
                return default;
            }

            return new string(buffer);
        }

        protected virtual string? ReadEntry()
        {
            var value = _reader.Read();

            return value == -1 ? default : value.ToString();
        }

        protected virtual string GetDelimiter() => _options.GetDelimiterOrDefault(string.Empty);
        
        protected IEnumerable<string> TransformIfPossible(StringBuilder currentEntryBuilder, string? current)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                yield break;
            }

            if (!currentEntryBuilder.TryBuildTransformableValue(current, ref _currentDepth, out var transformableValue) && transformableValue != null)
            {
                yield break;
            }

            var result = _transformer.Transform(transformableValue);

            if (result is null)
            {
                // TODO: Log and/or throw
                yield break;
            }

            if (EntryHandledCount > 0)
            {
                yield return GetDelimiter();
            }

            yield return result;

            Interlocked.Increment(ref _entryHandledCount);
        }

        public IEnumerable<string> WriteAllEntries(IEnumerable<string?> entries)
        {
            if (_options.HasOuterJsonScope())
            {
                yield return StreamChar.ArrayStart.ToString();
            }

            var currentEntryBuilder = new StringBuilder();
            var depth = 0;

            foreach(var current in entries)
            {
                
            }

            if (_options.HasOuterJsonScope())
            {
                yield return StreamChar.ArrayEnd.ToString();
            }
        }
    }
}
