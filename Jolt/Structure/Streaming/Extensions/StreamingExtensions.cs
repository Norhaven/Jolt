using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jolt.Structure.Streaming.Extensions
{
    internal static class StreamingExtensions
    {
        public static bool HasOuterJsonScope(this StreamingOptions options)
        {
            return options.OutputFormat == StreamingOutputFormat.ArrayWithMultiLinePerObject ||
                   options.OutputFormat == StreamingOutputFormat.ArrayWithSingleLinePerObject;
        }

        public static string GetDelimiterOrDefault(this StreamingOptions options, string delimiter)
        {
            return options.Delimiter switch
            {
                StreamingDelimiter.NewLine => "\n",
                StreamingDelimiter.Comma => ",",
                StreamingDelimiter.Rfc7464 => (0x1E).ToString(),
                StreamingDelimiter.None => string.Empty,
                StreamingDelimiter.Default => delimiter,
                _ => throw new ArgumentOutOfRangeException(nameof(delimiter), $"Unable to get delimiter for unknown delimiter type '{options.Delimiter}'")
            };
        }

        public static StreamReader GetReader(this Stream stream)
        {   
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return new StreamReader(stream, Encoding.UTF8, true, StreamSettings.ReaderBufferSize, true);
        }

        public static StreamWriter GetWriter(this Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return new StreamWriter(stream, Encoding.UTF8, StreamSettings.WriterBufferSize, true);
        }

        public static bool TryBuildTransformableValue(this StringBuilder entryBuilder, string currentValue, ref int depth, out string? transformableValue)
        {
            transformableValue = default;

            // In the case where we aren't reading a single character at a time,
            // we're reading by line and so there's nothing to build up (we already have it).

            if (currentValue.Length > 1)
            {
                transformableValue = currentValue;
                return true;
            }

            entryBuilder.Append(currentValue);

            if (currentValue == StreamChar.ObjectStart.ToString())
            {
                depth++;
            }
            else if (currentValue == StreamChar.ObjectEnd.ToString())
            {
                depth--;

                if (depth > 0)
                {
                    return false;
                }
                else
                {
                    transformableValue = entryBuilder.ToString();
                    entryBuilder.Clear();

                    return true;
                }
            }

            return false;
        }
    }
}
