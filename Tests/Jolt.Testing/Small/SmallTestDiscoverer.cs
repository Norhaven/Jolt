using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Structure;
using Jolt.Testing.Json;
using Jolt.Testing.Json.Attributes;
using Jolt.Testing.Small.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Jolt.Testing.Small
{
    internal sealed class SmallTestDiscoverer : TestCaseDiscoverer<SmallTest>
    {
        // Allow parameterless constructor for xUnit instantiation
        public SmallTestDiscoverer() : this(new NullMessageSink()) { }

        public SmallTestDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IEnumerable<SmallTest> ReadTestCaseDataFromSource(ITestContext testContext, TestType testType, IAttributeInfo attribute, ITestMethod testMethod)
        {
            var testContainerType = GetProperty<Type>(nameof(SmallTestAttribute.TestContainerType), attribute);
            var reader = testContext.CreateJsonContext(testType).JsonTokenReader;


            var testMethods = (from method in testContainerType.GetMethods()
                               let sourceAttribute = (SourceHasAttribute)method.GetCustomAttributes(typeof(SourceHasAttribute), true).FirstOrDefault()
                               let transformerAttribute = (TransformerIsAttribute)method.GetCustomAttributes(typeof(TransformerIsAttribute), true).FirstOrDefault()
                               let expectsResultAttribute = (ExpectsResultAttribute)method.GetCustomAttributes(typeof(ExpectsResultAttribute), true).FirstOrDefault()
                               let expectsExceptionAttribute = (ExpectsExceptionAttribute)method.GetCustomAttributes(typeof(ExpectsExceptionAttribute), true).FirstOrDefault()
                               where sourceAttribute != null && transformerAttribute != null && (expectsResultAttribute != null || expectsExceptionAttribute != null)
                               select (Method: method, Source: sourceAttribute, Transformer: transformerAttribute, ExpectsResult: expectsResultAttribute, ExpectsException: expectsExceptionAttribute))
                              .ToArray();

            var testIndex = 0;

            foreach (var (method, source, transformer, expectsResult, expectsException) in testMethods)
            {
                if (source == null || transformer == null)
                {
                    LogDiagnostic($"[DISCOVERY] [WARNING] Skipping method '{method.Name}' due to missing attributes");
                    continue;
                }

                LogDiagnostic($"[DISCOVERY] Found test using source '{source.Value}' and transformer '{transformer.ValueExpression}'");

                var sourceJson = reader.Read("{}") as IJsonObject;
                var transformerJson = reader.Read("{}") as IJsonObject;

                switch (source.Type)
                {
                    case SourceValueType.Object: sourceJson[source.Name] = reader.Read(source.Value?.ToString()); break;
                    default: sourceJson[source.Name] = reader.CreateTokenFrom(source.Value); break;
                };

                transformerJson[transformer.NameExpression] = reader.CreateTokenFrom(transformer.ValueExpression);

                var test = new SmallTest(_diagnosticMessageSink, testContext, testType, testContainerType.Name, method.Name, testIndex, sourceJson.ToTypeOf<string>(), transformerJson.ToTypeOf<string>(), expectsResult, expectsException);

                LogDiagnostic($"[DISCOVERY] [INFO] Discovered test method '{method.Name}' with index '{testIndex}' and source '{source.Value}' and transformer '{transformer.ValueExpression}'");

                yield return test;

                testIndex++;
            }
        }
    }
}
