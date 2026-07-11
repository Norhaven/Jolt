using Jolt.Structure.Streaming.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jolt.Structure.Streaming
{
    internal sealed class JoltStreamingTransformer<TContext> where TContext : IJsonContext
    {
        private readonly JoltTransformer<TContext> _transformer;
        private readonly StreamingOptions _streamingOptions;

        public JoltStreamingTransformer(JoltTransformer<TContext> transformer, StreamingOptions options)
        {
            _transformer = transformer;
            _streamingOptions = options;
        }

        public void TransformLines(Stream input, Stream output)
        {
            using var reader = input.GetReader();
            using var writer = output.GetWriter();

            TransformLines(reader, writer);
        }

        public void TransformLines(TextReader input, TextWriter output)
        {
            while (input.Peek() != StreamChar.EndOfStream)
            {
                var line = input.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var transformed = _transformer.Transform(line);

                if (transformed is null)
                {
                    continue;
                }

                output.WriteLine(transformed);
            }
        }

        public async Task TransformSequenceAsync(Stream input, Stream output, CancellationToken? cancellationToken = default)
        {
            using var reader = input.GetReader();
            using var writer = output.GetWriter();

            await TransformSequenceAsync(reader, writer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task TransformSequenceAsync(TextReader reader, TextWriter writer, CancellationToken? cancellationToken = default)
        {
            // Consume leading whitespace so Peek() lands on the discriminator.

            await ConsumeLeadingWhiteSpaceAsync(reader).ConfigureAwait(continueOnCapturedContext: false);

            switch (reader.Peek())
            {
                case StreamChar.Rfc7463RecordSeparator: ReadRfc7464Sequence(reader, writer); break;
                case StreamChar.ArrayStart: ReadArraySequence(reader, writer); break;
                case StreamChar.ObjectStart: ReadConcatenatedObjects(reader, writer); break;
                case StreamChar.EndOfStream: return;
                default:
                    {
                        var result = _transformer.Transform(reader.ReadToEnd());

                        if (result != null)
                        {
                            await writer.WriteAsync(result).ConfigureAwait(continueOnCapturedContext: false);
                        }

                        break;
                    }
            }
        }

        public void TransformSequence(TextReader reader, TextWriter writer)
        {
            // Consume leading whitespace so Peek() lands on the discriminator.

            ConsumeLeadingWhiteSpace(reader);

            switch (reader.Peek())
            {
                case StreamChar.Rfc7463RecordSeparator: ReadRfc7464Sequence(reader, writer); break;
                case StreamChar.ArrayStart: ReadArraySequence(reader, writer); break;
                case StreamChar.ObjectStart: ReadConcatenatedObjects(reader, writer); break;
                case StreamChar.EndOfStream: return;
                default:
                {
                    var result = _transformer.Transform(reader.ReadToEnd());

                    if (result != null)
                    {
                        writer.Write(result);
                    }

                    break;
                }
            }
        }

        private async Task ConsumeLeadingWhiteSpaceAsync(TextReader reader)
        {
            var buffer = new char[1];

            while (reader.Peek() is int ws)
            {
                if (ws == StreamChar.EndOfStream)
                {
                    return;
                }

                if (ws != StreamChar.WhiteSpace && ws != StreamChar.Tab && ws != StreamChar.CarriageReturn && ws != StreamChar.NewLine)
                {
                    await reader.ReadAsync(buffer, 0, 1).ConfigureAwait(continueOnCapturedContext: false);
                    continue;
                }

                break;
            }
        }

        private void ConsumeLeadingWhiteSpace(TextReader reader)
        {
            while (reader.Peek() is int ws)
            {
                if (ws == StreamChar.EndOfStream)
                {
                    return;
                }

                if (ws != StreamChar.WhiteSpace && ws != StreamChar.Tab && ws != StreamChar.CarriageReturn && ws != StreamChar.NewLine)
                {
                    reader.Read();
                    continue;
                }

                break;
            }
        }

        public void TransformSequence(Stream input, Stream output)
        {
            using var reader = input.GetReader();
            using var writer = output.GetWriter();

            TransformSequence(reader, writer);
        }

        private void ReadRfc7464Sequence(TextReader reader, TextWriter writer)
        {
            var builder = new StringBuilder();

            int current;

            while ((current = reader.Read()) != StreamChar.EndOfStream)
            {
                if (current == StreamChar.Rfc7463RecordSeparator)
                {
                    // Flush accumulated record (strip trailing LF/whitespace)

                    var json = builder.ToString().TrimEnd();

                    if (json.Length > 0)
                    {
                        var result = _transformer.Transform(json);

                        if (result != null)
                        {
                            writer.WriteLine(result);
                        }
                    }

                    builder.Clear();
                }
                else
                {
                    builder.Append((char)current);
                }
            }

            // Handle final record with no trailing separator.

            if (builder.Length > 0)
            {
                var resultJson = _transformer.Transform(builder.ToString().TrimEnd());

                if (resultJson != null)
                {
                    writer.WriteLine(resultJson);
                }
            }
        }

        private void ReadArraySequence(TextReader reader, TextWriter writer)
        {
            // Read the beginning array character.

            reader.Read();

            // Read each object separated by commas or finished with a closing array character.

            var builder = new StringBuilder();
            int depth = 1;
            int current;
            bool isFirstEntry = true;
            bool isArrayOutput = _streamingOptions.OutputFormat == StreamingOutputFormat.Default ||
                                 _streamingOptions.OutputFormat == StreamingOutputFormat.ArrayWithMultiLinePerObject ||
                                 _streamingOptions.OutputFormat == StreamingOutputFormat.ArrayWithSingleLinePerObject;

            if (isArrayOutput)
            {
                writer.Write((char)StreamChar.ArrayStart);
            }

            while ((current = reader.Read()) != StreamChar.EndOfStream)
            {
                builder.Append((char)current);

                if (current == StreamChar.ObjectStart)
                {
                    depth++;
                }                
                else if (current == StreamChar.ObjectEnd)
                {
                    depth--;

                    if (depth == 1)
                    {
                        var resultJson = _transformer.Transform(builder.ToString());

                        if (resultJson != null)
                        {
                            if (!isFirstEntry)
                            {
                                if (_streamingOptions.Delimiter == StreamingDelimiter.Default)
                                {
                                    writer.Write((char)StreamChar.Comma);
                                }
                                else
                                {
                                    writer.Write(_streamingOptions.GetDelimiterOrDefault(StreamChar.Comma.ToString()));
                                }
                            }

                            isFirstEntry = false;
                            writer.WriteLine(resultJson);
                            builder.Clear();

                            ConsumeLeadingWhiteSpace(reader);

                            if (reader.Peek() == StreamChar.Comma)
                            {
                                reader.Read();
                                continue;
                            }
                            else if (current == StreamChar.ArrayEnd)
                            {
                                reader.Read();
                                break;
                            }
                        }
                    }
                }
            }

            if (isArrayOutput)
            {
                writer.Write((char)StreamChar.ArrayEnd);
            }
        }

        private void ReadConcatenatedObjects(TextReader reader, TextWriter writer)
        {
            var builder = new StringBuilder();
            int depth = 0;
            int current;

            while ((current = reader.Read()) != StreamChar.EndOfStream)
            {
                builder.Append((char)current);

                if (current == StreamChar.ObjectStart)
                {
                    depth++;
                }
                else if (current == StreamChar.ObjectEnd)
                {
                    depth--;

                    if (depth == 0)
                    {
                        var resultJson = _transformer.Transform(builder.ToString());

                        if (resultJson != null)
                        {
                            writer.WriteLine(resultJson); 
                            builder.Clear();
                        }
                    }
                }
            }
        }

        public async Task TransformLinesAsync(Stream input, Stream output, CancellationToken? cancellationToken = default)
        {
            cancellationToken = cancellationToken ?? CancellationToken.None;

            using var reader = new StreamReader(input);
            using var writer = new StreamWriter(output, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            await TransformLinesAsync(reader, writer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task TransformLinesAsync(TextReader input, TextWriter output, CancellationToken? cancellationToken = default)
        {
            cancellationToken = cancellationToken ?? CancellationToken.None;

            var currentLine = await input.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);

            while (currentLine != null)
            {
                var transformed = _transformer.Transform(currentLine);

                if (transformed is null)
                {
                    continue;
                }

                await output.WriteLineAsync(transformed).ConfigureAwait(continueOnCapturedContext: false);

                if (cancellationToken.Value.IsCancellationRequested)
                {
                    break;
                }

                currentLine = await input.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}