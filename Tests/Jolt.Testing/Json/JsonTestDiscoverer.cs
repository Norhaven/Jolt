using Jolt.Testing.Json.Attributes;
using Jolt.Testing.Resources;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Jolt.Testing.Json
{
    /// <summary>
    /// Custom xUnit test discoverer that creates individual test case entries for each JSON test
    /// instead of nesting them under a theory. This provides a better Test Explorer experience.
    /// </summary>
    public sealed class JsonTestDiscoverer : TestCaseDiscoverer<EndToEndTest>
    {
        // Allow parameterless constructor for xUnit instantiation
        public JsonTestDiscoverer() : this(new NullMessageSink()) { }

        public JsonTestDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IEnumerable<EndToEndTest> ReadTestCaseDataFromSource(ITestContext testContext, TestType testType, IAttributeInfo attribute, ITestMethod testMethod)
        {
            var testJsonFile = GetProperty<string>(nameof(JsonTestAttribute.TestJsonFile), attribute);

            if (string.IsNullOrWhiteSpace(testJsonFile))
            {
                throw new InvalidOperationException($"Test file '{nameof(JsonTestAttribute.TestJsonFile)}' should specify a file name");
            }

            LogDiagnostic($"[DISCOVERY] Reading and parsing test file: '{testJsonFile}'");

            var context = testContext.CreateJsonContext(testType);
            var testJsonData = ReadAndParseTestFile(testJsonFile, context);

            var testIndex = 1;

            foreach (var testGroup in testJsonData.TestGroups ?? Array.Empty<TestGroup>())
            {
                foreach (var test in testGroup.Tests ?? Array.Empty<EndToEndTest>())
                {
                    LogDiagnostic($"[DISCOVERY] Populating test case with data from JSON test");

                    test.TestIndex = testIndex;
                    test.TestGroup = testGroup.Name;
                    test.TestContext = testContext;
                    test.TestType = testType;
                    test.Source = testGroup.Source;
                    test.PossibleExceptions = testJsonData.PossibleExceptionCodes;
                    test.PossibleExternalMethodSources = testJsonData.PossibleExternalMethodSources;
                    test.ExternalMethodSource = testGroup.ExternalMethodSource;

                    yield return test;

                    testIndex++;
                }
            }

            LogDiagnostic($"[DISCOVERY] Total test cases discovered: {testIndex}");
        }
        
        private void LogDiagnostic(string message)
        {
            Trace.WriteLine(message);
            _diagnosticMessageSink.OnMessage(new DiagnosticMessage(message));
        }

        private TestFile ReadAndParseTestFile(string testFileName, IJsonContext context)
        {
            var testFileJson = TestResource.ReadTestFile(testFileName);
            var schemaJson = TestResource.ReadTestFile("JsonTests.JsonTest.schema");

            var schema = JsonSchema.FromJsonAsync(schemaJson).Result;
            var evaluationResults = schema.Validate(testFileJson);

            if (evaluationResults.Count > 0)
            {
                var resultsText = string.Join(Environment.NewLine, evaluationResults);
                throw new InvalidOperationException($"Test file '{testFileName}' does not match schema:\n{resultsText}");
            }

            return context.JsonTokenReader.Read(testFileJson).ToTypeOf<TestFile>();
        }
    }
}
