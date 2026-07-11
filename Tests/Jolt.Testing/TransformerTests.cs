using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Resources;
using Jolt.Testing.Transformers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Testing
{
    public abstract class TransformerTests : TestContainer
    {
        protected enum TestExecutionType
        {
            SynchronousString,
            SynchronousStreamWithLines,
            AsynchronousStreamWithLines,
            SynchronousReaderWriterWithLines,
            AsynchronousReaderWriterWithLines,
            SynchronousStreamWithSequences,
            SynchronousReaderWriterWithSequences,
            AsynchronousStreamWithSequences,
            AsynchronousReaderWriterWithSequences
        }

        protected async Task<IJsonObject> ExecuteTestForObjectResult(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default, JoltOptions options = default, params string[] partialTransformerNames)
        {
            var result = await ExecuteTestIfPossible(test, configureContext, testMethodName, partialTransformerNames, TestExecutionType.SynchronousString, options);

            return result.AsObject();
        }

        protected Task<IJsonToken> ExecuteTest(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default, TestExecutionType executionType = TestExecutionType.SynchronousString, JoltOptions options = default, string customSource = default, params string[] partialTransformerNames)
        {
            return ExecuteTestIfPossible(test, configureContext, testMethodName, partialTransformerNames, executionType, options, customSource);
        }

        private async Task<IJsonToken> ExecuteTestIfPossible(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext, string testMethodName, string[] partialTransformerNames, TestExecutionType executionType, JoltOptions options, string customSource = default)
        {
            var method = GetType().GetMethod(testMethodName);

            if (method is null)
            {
                throw new ArgumentNullException(nameof(testMethodName), $"Unable to locate test method '{testMethodName}'");
            }

            var jsonContext = test.TestContext.CreateJsonContext(test.TestType, options);

            var context = configureContext == null ? jsonContext : configureContext(jsonContext);

            context = context
                .UseTransformer(test.Transformer)
                .RegisterAllMethodsFrom(test.ExternalMethodsType);

            if (partialTransformerNames != null)
            {
                foreach (var partialTransformerName in partialTransformerNames)
                {
                    var partialTransformer = TestResource.ReadTransformer(partialTransformerName);
                    context = context.RegisterTransformer(new TransformerRegistration(partialTransformerName, partialTransformer));
                }
            }

            var type = test.ExternalMethodsType;

            if (type != null)
            {
                // If it's not a static class, go ahead and instantiate it anyway just in case there's some instance
                // methods that will come along for the ride here.

                var isStaticClass = type.IsClass && type.IsAbstract && type.IsSealed;

                if (!isStaticClass)
                {
                    context = context.UseMethodContext(Activator.CreateInstance(test.ExternalMethodsType));
                }
            }

            var transformer = new JoltTransformer<IJsonContext>(context);

            var transformedDocument = await TransformWith(transformer, customSource ?? test.Source, executionType);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            if (options?.IsExecutionTracingEnabled == true)
            {
                var traceResults = context.JsonTokenReader.Read("[]");

                foreach (var trace in context.MessageProvider.ExecutionTraces.OrderBy(x => x.ExecutionStartedAtTicks))
                {
                    var traceResult = context.JsonTokenReader.Read("{}").AsObject();

                    var traceEntries = from property in trace.GetType().GetProperties()
                                       let value = property.GetValue(trace)
                                       select (property.Name, context.JsonTokenReader.CreateTokenFrom(value));

                    foreach (var (name, value) in traceEntries)
                    {
                        traceResult[name] = value;
                    }

                    traceResults.AsArray().Add(traceResult);
                }

                return traceResults;
            }

            if (executionType == TestExecutionType.SynchronousString)
            {
                return context.JsonTokenReader.Read(transformedDocument) as IJsonObject;
            }

            var lines = transformedDocument
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => context.JsonTokenReader.Read(x));

            return context.JsonTokenReader.CreateArrayFrom(lines);
        }

        private async Task<string> TransformWith(IJsonTransformer<IJsonContext> transformer, string sourceJson, TestExecutionType executionType)
        {
            switch (executionType)
            {
                case TestExecutionType.SynchronousString: return transformer.Transform(sourceJson);
                case TestExecutionType.SynchronousStreamWithLines: return TransformLinesWithStream(transformer, sourceJson);
                case TestExecutionType.AsynchronousStreamWithLines: return await TransformLinesWithStreamAsync(transformer, sourceJson);
                case TestExecutionType.SynchronousReaderWriterWithLines: return TransformLinesWithReaderWriter(transformer, sourceJson);
                case TestExecutionType.AsynchronousReaderWriterWithLines: return await TransformLinesWithReaderWriterAsync(transformer, sourceJson);
                case TestExecutionType.SynchronousStreamWithSequences: return TransformSequenceWithStream(transformer, sourceJson);
                case TestExecutionType.AsynchronousStreamWithSequences: return await TransformSequenceWithStreamAsync(transformer, sourceJson);
                case TestExecutionType.SynchronousReaderWriterWithSequences: return TransformSequenceWithReaderWriter(transformer, sourceJson);
                case TestExecutionType.AsynchronousReaderWriterWithSequences: return await TransformSequenceWithReaderWriterAsync(transformer, sourceJson);
                default: throw new NotSupportedException($"The specified execution type '{executionType}' is not supported.");
            }
        }

        private string TransformLinesWithStream(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithStream(sourceJson, (input, output) => transformer.TransformLines(input, output));
        }

        private Task<string> TransformLinesWithStreamAsync(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithStreamAsync(sourceJson, (input, output) => transformer.TransformLinesAsync(input, output));
        }

        private string TransformLinesWithReaderWriter(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithReaderWriter(sourceJson, (reader, writer) => transformer.TransformLines(reader, writer));
        }

        private Task<string> TransformLinesWithReaderWriterAsync(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithReaderWriterAsync(sourceJson, (reader, writer) => transformer.TransformLinesAsync(reader, writer));
        }

        private string TransformSequenceWithStream(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithStream(sourceJson, (input, output) => transformer.TransformSequence(input, output));
        }

        private Task<string> TransformSequenceWithStreamAsync(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithStreamAsync(sourceJson, (input, output) => transformer.TransformSequenceAsync(input, output));
        }

        private string TransformSequenceWithReaderWriter(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithReaderWriter(sourceJson, (reader, writer) => transformer.TransformSequence(reader, writer));
        }

        private Task<string> TransformSequenceWithReaderWriterAsync(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            return TransformWithReaderWriterAsync(sourceJson, (reader, writer) => transformer.TransformSequenceAsync(reader, writer));
        }

        private string TransformWithStream(string sourceJson, Action<Stream, Stream> transformStream)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceJson)))
            using (var outputStream = new MemoryStream())
            {
                transformStream(inputStream, outputStream);

                outputStream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(outputStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private async Task<string> TransformWithStreamAsync(string sourceJson, Func<Stream, Stream, Task> transformStreamAsync)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceJson)))
            using (var outputStream = new MemoryStream())
            {
                await transformStreamAsync(inputStream, outputStream);

                outputStream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(outputStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private string TransformWithReaderWriter(string sourceJson, Action<TextReader, TextWriter> transformReaderWriter)
        {
            using (var inputReader = new StringReader(sourceJson))
            using (var outputWriter = new StringWriter())
            {
                transformReaderWriter(inputReader, outputWriter);

                return outputWriter.ToString();
            }
        }

        private async Task<string> TransformWithReaderWriterAsync(string sourceJson, Func<TextReader, TextWriter, Task> transformReaderWriterAsync)
        {
            using (var inputReader = new StringReader(sourceJson))
            using (var outputWriter = new StringWriter())
            {
                await transformReaderWriterAsync(inputReader, outputWriter);

                return outputWriter.ToString();
            }
        }
    }
}
