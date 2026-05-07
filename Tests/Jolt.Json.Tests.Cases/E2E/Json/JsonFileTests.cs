using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Structure;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Jolt.Json.Tests.Cases.E2E.Json
{
    public abstract class JsonFileTests : JsonTest
    {        
        //[JsonTestDefinition("IndexingWithVariables")]
        //public void IndexingWithVariablesTests() { }

        //[JsonTestDefinition("LibraryMethodsWithVariables")]
        //public void LibraryMethodsWithVariablesTests() { }

        //[JsonTestDefinition("ExternalMethodsTests")]
        //public void ExternalMethodsTests() { }

        //[JsonTestDefinition("OperatorsAndVariablesTests")]
        //public void OperatorsAndVariablesTests_WillSucceed() { }

        /// <summary>
        /// Tests for standard library methods that work with range variables.
        /// Uses reflection to generate individual test methods dynamically, creating
        /// distinct Test Explorer entries for each JSON test case.
        /// </summary>
        //public abstract class JsonTests : IDisposable
        //{
            private static TestFile _cachedTestFile;

            private static TestFile LoadTestFile(IJsonContext context, string testFile)
            {
                if (_cachedTestFile != null)
                {
                    return _cachedTestFile;
                }

                var testFileJson = ReadEmbeddedJson(testFile);
                var schemaJson = ReadEmbeddedJson("JsonTest.schema");

                var schema = JsonSchema.FromJsonAsync(schemaJson).Result;
                var evaluationResults = schema.Validate(testFileJson);

                if (evaluationResults.Count > 0)
                {
                    var resultsText = string.Join(Environment.NewLine, evaluationResults);
                    throw new InvalidOperationException($"Test file validation failed:\n{resultsText}");
                }

                _cachedTestFile = context.JsonTokenReader.Read(testFileJson).ToTypeOf<TestFile>();

                return _cachedTestFile;
            }

            /// <summary>
            /// Generates individual test facts for each JSON test case.
            /// This creates distinct entries in Test Explorer for each test.
            /// </summary>
            public static IEnumerable<object[]> GetJsonTestCases(Type jsonData, string testFileName)
            {
                var testDefinition = Activator.CreateInstance(jsonData) as JsonTestDefinitionAttribute;
                var testContext = testDefinition.Context;

                if (testContext is null)
                {
                    throw new InvalidOperationException($"Unable to get test cases without the '{nameof(UsesJsonContextAttribute)}' on the test class.");
                }

                var testFile = LoadTestFile(testContext, testFileName);

                int testIndex = 0;

                foreach (var testGroup in testFile.TestGroups)
                {
                    foreach (var test in testGroup.Tests)
                    {
                        yield return new object[]
                        {
                            testIndex++,
                            testGroup.Name,
                            test.Name,
                            testGroup.Source.ToString(),
                            test.Transformer,
                            test.Result,
                            test.ExceptionCode,
                            test.ExceptionType,
                            test.InnerExceptionCode,
                            test.ExternalMethodSource ?? testGroup.ExternalMethodSource,
                            testFile.PossibleExceptionCodes ?? new Dictionary<string, string>(),
                            testFile.PossibleExternalMethodSources ?? new Dictionary<string, string>()
                        };
                    }
                }
            }

            //[Theory(DisplayName = "Library Methods With Variables")]
            //[MemberData(nameof(GetJsonTestCases))]
            public void LibraryMethodsWithVariables(
                int testIndex,
                string groupName,
                string testName,
                string source,
                IJsonObject transformer,
                IJsonObject result,
                string exceptionCode,
                string exceptionType,
                string innerExceptionCode,
                string externalMethodSource,
                Dictionary<string, string> possibleExceptions,
                Dictionary<string, string> possibleExternalMethodSources,
                IJsonContext context)
            {
                context.ReferenceResolver.Clear();

                var builtContext = context.UseTransformer(transformer.ToString());

                if (!string.IsNullOrWhiteSpace(externalMethodSource))
                {
                    var methodSourceName = externalMethodSource;
                    if (!possibleExternalMethodSources.TryGetValue(externalMethodSource, out var mappedSource))
                    {
                        methodSourceName = externalMethodSource;
                    }
                    else
                    {
                        methodSourceName = mappedSource;
                    }

                    var externalMethodType = Type.GetType(methodSourceName);
                    if (externalMethodType != null)
                    {
                        builtContext = builtContext.RegisterAllMethodsFrom(externalMethodType);
                        var isStaticClass = externalMethodType.IsClass && externalMethodType.IsAbstract && externalMethodType.IsSealed;
                        if (!isStaticClass)
                        {
                            builtContext = builtContext.UseMethodContext(Activator.CreateInstance(externalMethodType));
                        }
                    }
                }

                var transformer_ = new JoltTransformer<IJsonContext>(builtContext);

                try
                {
                    var transformResult = transformer_.Transform(source);
                    var resultToken = context.JsonTokenReader.Read(transformResult);

                    var isEqual = result.DeepEquals(resultToken, new JsonTest.JsonTestContainer.TestDoubleJsonEqualityComparer());

                    isEqual.Should().BeTrue(
                        $"[{groupName} :: {testName}] Transformation failed.\n" +
                        $"Expected:\n{result}\n\n" +
                        $"Actual:\n{transformResult}");
                }
                catch (JoltException ex) when (exceptionCode != null || exceptionType != null)
                {
                    if (exceptionCode != null)
                    {
                        var expectedCodeName = possibleExceptions.TryGetValue(exceptionCode, out var mapped) ? mapped : exceptionCode;
                        var expectedCode = (ExceptionCode)Enum.Parse(typeof(ExceptionCode), expectedCodeName);
                        ex.Code.Should().Be(expectedCode, $"[{groupName} :: {testName}] Expected exception code {expectedCode}");
                    }

                    if (exceptionType != null)
                    {
                        ex.GetType().Name.Should().Be(exceptionType, $"[{groupName} :: {testName}] Expected exception type {exceptionType}");
                    }
                }
            }

            private static string ReadEmbeddedJson(string fileName)
            {
                var resourceName = $"Jolt.Json.Tests.Resources.TestFiles.JsonTests.{fileName}.json";
                using (var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (manifestStream == null)
                        throw new FileNotFoundException($"Resource '{resourceName}' not found");

                    using (var reader = new StreamReader(manifestStream))
                        return reader.ReadToEnd();
                }
            }

            public void Dispose()
            {
                
            }
        }
    }
//}
