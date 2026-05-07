using Jolt.Testing.Attributes;
using Jolt.Testing.Json;
using Jolt.Testing.Json.Attributes;
using Jolt.Testing.Small;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Jolt.Testing
{
    public abstract class TestCaseDiscoverer<T> : IXunitTestCaseDiscoverer where T : TestData, new()
    {
        protected readonly IMessageSink _diagnosticMessageSink;

        // Allow parameterless constructor for xUnit instantiation
        public TestCaseDiscoverer() : this(new NullMessageSink()) { }

        public TestCaseDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink ?? new NullMessageSink();
        }

        public virtual IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo attribute)
        {
            LogDiagnostic($"[DISCOVERY] Starting discovery for method: {testMethod.Method.Name}");
            LogDiagnostic($"[DISCOVERY] Using TestMethod type '{testMethod.GetType()}' and AttributeInfo type '{attribute.GetType()}'");

            var testCases = new List<IXunitTestCase>();

            try
            {
                var testContext = GetProperty<ITestContext>(nameof(TestDiscoveryAttribute.TestContext), attribute);
                var testType = GetProperty<TestType>(nameof(TestDiscoveryAttribute.TestType), attribute);

                var testCaseData = ReadTestCaseDataFromSource(testContext, testType, attribute, testMethod).ToArray();

                if (testCaseData.Length == 0)
                {
                    LogDiagnostic($"[DISCOVERY] No test cases discovered within discoverer of type '{this.GetType().FullName}'.");
                    return Array.Empty<IXunitTestCase>();
                }

                foreach (var data in testCaseData)
                {
                    var testCase = new JoltTestCase<T>(
                           data,
                           _diagnosticMessageSink,
                           discoveryOptions,
                           testMethod);

                    testCases.Add(testCase);
                }

                return testCases;
            }
            catch (Exception ex)
            {
                LogDiagnostic($"[DISCOVERY][ERROR] {ex.Message}");
                return Array.Empty<IXunitTestCase>();
            }
            finally
            {
                LogDiagnostic("[DISCOVERY] Discovery complete!");
            }
        }

        protected abstract IEnumerable<T> ReadTestCaseDataFromSource(ITestContext testContext, TestType testType, IAttributeInfo attribute, ITestMethod testMethod);

        protected TData GetProperty<TData>(string name, IAttributeInfo attribute)
        {
            var value = attribute.GetNamedArgument<TData>(name);

            if (value == null)
            {
                LogDiagnostic($"[DISCOVERY] [ERROR]: Unable to retrieve property with name '{name}' from attribute");
                return default;
            }

            return value;
        }

        protected void LogDiagnostic(string message)
        {
            Trace.WriteLine(message);
            _diagnosticMessageSink.OnMessage(new DiagnosticMessage(message));
        }
    }
}
