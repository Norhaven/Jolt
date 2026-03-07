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
        public abstract class TransformerTestContainer : TestContainer<TransformerTest>
        {
            public TransformerTestContainer(MethodInfo testMethod, TransformerTestDefinitionAttribute attribute)
                : base(testMethod, attribute)
            {
            }

            public override void Execute(TransformerTest testInstance)
            {
                _method.Invoke(testInstance, new[] { this });
            }

            public override IEnumerable<object[]> GetTestsFromContainer()
            {
                yield break;
            }
        }

        protected IJsonObject ExecuteTest(TransformerTestContainer container, Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default)
        {
            return ExecuteTestIfPossible(container, configureContext, testMethodName);
        }

        private IJsonObject ExecuteTestIfPossible(TransformerTestContainer container, Func<IJsonContext, IJsonContext> configureContext, string testMethodName)
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

            var context = configureContext == null ? container.Context : configureContext(container.Context);

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

            return context.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        protected IJsonObject ExecuteTestFor<T>(TransformerTestContainer container, string transformerJson, string testDocumentJson, object methodContext = default)
        {
            var transformer = CreateTransformerWith<T>(container, transformerJson, methodContext);
            var transformedDocument = transformer.Transform(testDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return container.Context.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        protected IJsonObject ExecuteTestFor(TransformerTestContainer container, string transformerJson, string testDocumentJson, IEnumerable<MethodRegistration> methodRegistrations = default, object methodContext = default)
        {
            var transformer = CreateTransformerWith(container, transformerJson, methodRegistrations, methodContext);
            var transformedDocument = transformer.Transform(testDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return container.Context.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        private IJsonTransformer<IJsonContext> CreateTransformerWith<T>(TransformerTestContainer container, string transformerJson, object methodContext = default)
        {
            var context = container.Context
                .UseTransformer(transformerJson)
                .RegisterAllMethodsFrom<T>()
                .UseMethodContext(methodContext);

            return new JoltTransformer<IJsonContext>(context);
        }

        private IJsonTransformer<IJsonContext> CreateTransformerWith(TransformerTestContainer container, string transformerJson, IEnumerable<MethodRegistration> methodRegistrations, object methodContext = default)
        {
            var context = container.Context
                .UseTransformer(transformerJson)
                .RegisterAllMethods(methodRegistrations)
                .UseMethodContext(methodContext);

            return new JoltTransformer<IJsonContext>(context);
        }

        private static string ReadTestDocument(string fileName) => TestResource.ReadDocument(fileName);
        private static string ReadTestTransformer(string fileName) => TestResource.ReadTransformer(fileName);
    }
}
