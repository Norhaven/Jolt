using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jolt.Json.Tests.Resources
{
    public abstract class TransformerTest : Test
    {
        public sealed class TransformerTestContainer : TestContainer
        {
            public TransformerTestContainer(MethodInfo testMethod, TransformerTestDefinitionAttribute attribute)
                : base(testMethod, attribute)
            {
            }

            public void Execute(IJsonContext context, object testInstance)
            {
                _method.Invoke(testInstance, Array.Empty<object>());
            }

            public override void Execute(IJsonContext context)
            {
                throw new NotSupportedException($"Transformer tests have already been built around a standard xUnit testing pattern and so the overload which allows a test instance to be provided must be invoked instead for test method '{_method.Name}'");
            }
        }

        protected TransformerTest(IJsonContext context)
            : base(context)
        {
        }

        protected IJsonObject ExecuteTest(Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default)
        {
            return ExecuteTestIfPossible(configureContext, testMethodName);
        }

        private IJsonObject ExecuteTestIfPossible(Func<IJsonContext, IJsonContext> configureContext, string testMethodName)
        {
            var method = GetType().GetMethod(testMethodName);

            if (method is null)
            {
                throw new ArgumentNullException(nameof(testMethodName), $"Unable to locate test method '{testMethodName}'");
            }

            var definition = method.GetCustomAttribute<TransformerTestDefinitionAttribute>(inherit: false);

            if (definition is null)
            {
                throw new ArgumentNullException(nameof(testMethodName), $"Missing TestDefinitionAttribute on test method '{testMethodName}'");
            }

            var transformerJson = ReadTestTransformer(definition.TransformerName);
            var sourceDocumentJson = ReadTestDocument(definition.SourceDocumentName);

            var context = configureContext == null ? _testContext : configureContext(_testContext);

            context = context
                .UseTransformer(transformerJson)
                .RegisterAllMethodsFrom(definition.ExternalMethodType);

            var type = definition.ExternalMethodType;

            if (type != null)
            {
                // If it's not a static class, go ahead and instantiate it anyway just in case there's some instance
                // methods that will come along for the ride here.

                var isStaticClass = type.IsClass && type.IsAbstract && type.IsSealed;

                if (!isStaticClass)
                {
                    context = context.UseMethodContext(Activator.CreateInstance(definition.ExternalMethodType));
                }
            }

            var transformer = new JoltTransformer<IJsonContext>(context);

            var transformedDocument = transformer.Transform(sourceDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return _testContext.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        protected IJsonObject ExecuteTestFor<T>(string transformerJson, string testDocumentJson, object methodContext = default)
        {
            var transformer = CreateTransformerWith<T>(transformerJson, methodContext);
            var transformedDocument = transformer.Transform(testDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return _testContext.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        protected IJsonObject ExecuteTestFor(string transformerJson, string testDocumentJson, IEnumerable<MethodRegistration> methodRegistrations = default, object methodContext = default)
        {
            var transformer = CreateTransformerWith(transformerJson, methodRegistrations, methodContext);
            var transformedDocument = transformer.Transform(testDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return _testContext.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        private IJsonTransformer<IJsonContext> CreateTransformerWith<T>(string transformerJson, object methodContext = default)
        {
            var context = _testContext
                .UseTransformer(transformerJson)
                .RegisterAllMethodsFrom<T>()
                .UseMethodContext(methodContext);

            return new JoltTransformer<IJsonContext>(context);
        }

        private IJsonTransformer<IJsonContext> CreateTransformerWith(string transformerJson, IEnumerable<MethodRegistration> methodRegistrations, object methodContext = default)
        {
            var context = _testContext
                .UseTransformer(transformerJson)
                .RegisterAllMethods(methodRegistrations)
                .UseMethodContext(methodContext);

            return new JoltTransformer<IJsonContext>(context);
        }

        private static string ReadTestDocument(string fileName) => TestResource.ReadDocument(fileName);
        private static string ReadTestTransformer(string fileName) => TestResource.ReadTransformer(fileName);
    }
}
