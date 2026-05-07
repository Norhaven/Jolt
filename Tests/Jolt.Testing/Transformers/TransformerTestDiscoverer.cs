using Jolt.Testing.Json;
using Jolt.Testing.Resources;
using Jolt.Testing.Transformers.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Jolt.Testing.Transformers
{
    internal class TransformerTestDiscoverer : TestCaseDiscoverer<TransformerTest>
    {
        // Allow parameterless constructor for xUnit instantiation
        public TransformerTestDiscoverer() : this(new NullMessageSink()) { }

        public TransformerTestDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IEnumerable<TransformerTest> ReadTestCaseDataFromSource(ITestContext testContext, TestType testType, IAttributeInfo attribute, ITestMethod testMethod)
        {
            var context = testContext.CreateJsonContext(testType);

            var transformerFile = GetProperty<string>(nameof(TransformerTestAttribute.TransformerDocument), attribute);
            var sourceFile = GetProperty<string>(nameof(TransformerTestAttribute.SourceDocument), attribute);
            var externalMethodsType = GetProperty<Type>(nameof(TransformerTestAttribute.ExternalMethodsType), attribute);

            var transformer = TestResource.ReadTransformer(transformerFile);
            var source = TestResource.ReadDocument(sourceFile);

            if (string.IsNullOrWhiteSpace(transformer))
            {
                throw new InvalidOperationException($"Transformer file '{transformerFile}' read as empty");
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                throw new InvalidOperationException($"Source file '{sourceFile}' read as empty");
            }

            var test = new TransformerTest(transformer, source, externalMethodsType, _diagnosticMessageSink, testContext, testType, "Transformers", testMethod.Method.Name, -1);

            yield return test;
        }
    }
}
