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
            SynchronousStream,
            AsynchronousStream,
            SynchronousReaderWriter,
            AsynchronousReaderWriter
        }

        protected async Task<IJsonObject> ExecuteTestForObjectResult(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default, params string[] partialTransformerNames)
        {
            var result = await ExecuteTestIfPossible(test, configureContext, testMethodName, partialTransformerNames, TestExecutionType.SynchronousString);

            return result.AsObject();
        }

        protected Task<IJsonToken> ExecuteTest(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default, TestExecutionType executionType = TestExecutionType.SynchronousString, params string[] partialTransformerNames)
        {
            return ExecuteTestIfPossible(test, configureContext, testMethodName, partialTransformerNames, executionType);
        }

        private async Task<IJsonToken> ExecuteTestIfPossible(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext, string testMethodName, string[] partialTransformerNames, TestExecutionType executionType)
        {
            var method = GetType().GetMethod(testMethodName);

            if (method is null)
            {
                throw new ArgumentNullException(nameof(testMethodName), $"Unable to locate test method '{testMethodName}'");
            }

            var jsonContext = test.TestContext.CreateJsonContext(test.TestType);

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

            var transformedDocument = await TransformWith(transformer, test.Source, executionType);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
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
                case TestExecutionType.SynchronousStream: return TransformWithStream(transformer, sourceJson);
                case TestExecutionType.AsynchronousStream: return await TransformWithStreamAsync(transformer, sourceJson);
                case TestExecutionType.SynchronousReaderWriter: return TransformWithReaderWriter(transformer, sourceJson);
                case TestExecutionType.AsynchronousReaderWriter: return await TransformWithReaderWriterAsync(transformer, sourceJson);
                default: throw new NotSupportedException($"The specified execution type '{executionType}' is not supported.");
            }
        }

        private string TransformWithStream(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceJson)))
            using (var outputStream = new MemoryStream())
            {
                transformer.Transform(inputStream, outputStream);

                outputStream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(outputStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private async Task<string> TransformWithStreamAsync(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceJson)))
            using (var outputStream = new MemoryStream())
            {
                await transformer.TransformAsync(inputStream, outputStream);

                outputStream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(outputStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private string TransformWithReaderWriter(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            using (var inputReader = new StringReader(sourceJson))
            using (var outputWriter = new StringWriter())
            {
                transformer.Transform(inputReader, outputWriter);
                return outputWriter.ToString();
            }
        }

        private async Task<string> TransformWithReaderWriterAsync(IJsonTransformer<IJsonContext> transformer, string sourceJson)
        {
            using (var inputReader = new StringReader(sourceJson))
            using (var outputWriter = new StringWriter())
            {
                await transformer.TransformAsync(inputReader, outputWriter);

                return outputWriter.ToString();
            }
        }
    }
}
