using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Resources;
using Jolt.Testing.Transformers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jolt.Testing.Harness.DotNetFramework
{
    public abstract class TransformerTests : TestContainer
    {
        protected IJsonObject ExecuteTest(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext = default, [CallerMemberName] string testMethodName = default, params string[] partialTransformerNames)
        {
            return ExecuteTestIfPossible(test, configureContext, testMethodName, partialTransformerNames);
        }

        private IJsonObject ExecuteTestIfPossible(TransformerTest test, Func<IJsonContext, IJsonContext> configureContext, string testMethodName, string[] partialTransformerNames)
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

            var transformedDocument = transformer.Transform(test.Source);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return context.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        protected IJsonObject ExecuteTestFor<T>(TransformerTest test, string transformerJson, string testDocumentJson, object methodContext = default)
        {
            var transformer = CreateTransformerWith<T>(test, transformerJson, methodContext);
            var transformedDocument = transformer.Transform(testDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return test.JsonContext.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        protected IJsonObject ExecuteTestFor(TransformerTest test, string transformerJson, string testDocumentJson, IEnumerable<MethodRegistration> methodRegistrations = default, object methodContext = default)
        {
            var transformer = CreateTransformerWith(test, transformerJson, methodRegistrations, methodContext);
            var transformedDocument = transformer.Transform(testDocumentJson);

            if (transformedDocument == null)
            {
                throw new ArgumentException("Expected a transformed document because a valid test document was sent in and used by a valid transformer but found null");
            }

            return test.JsonContext.JsonTokenReader.Read(transformedDocument) as IJsonObject;
        }

        private IJsonTransformer<IJsonContext> CreateTransformerWith<T>(TransformerTest test, string transformerJson, object methodContext = default)
        {
            var context = test.JsonContext
                .UseTransformer(transformerJson)
                .RegisterAllMethodsFrom<T>()
                .UseMethodContext(methodContext);

            return new JoltTransformer<IJsonContext>(context);
        }

        private IJsonTransformer<IJsonContext> CreateTransformerWith(TransformerTest test, string transformerJson, IEnumerable<MethodRegistration> methodRegistrations, object methodContext = default)
        {
            var context = test.JsonContext
                .UseTransformer(transformerJson)
                .RegisterAllMethods(methodRegistrations)
                .UseMethodContext(methodContext);

            return new JoltTransformer<IJsonContext>(context);
        }
    }
}
