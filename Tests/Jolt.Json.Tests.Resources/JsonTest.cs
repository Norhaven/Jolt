using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Structure;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Jolt.Json.Tests.Resources
{
    public abstract class JsonTest : Test
    {
        public sealed class TestFile
        {
            public Dictionary<string, string> PossibleExceptionCodes { get; set; }
            public Dictionary<string, string> PossibleExternalMethodSources { get; set; }
            public TestGroup[] TestGroups { get; set; }
        }

        public sealed class TestGroup
        {
            public string Name { get; set; }
            public dynamic Source { get; set; }
            public string ExternalMethodSource { get; set; }
            public EndToEndTest[] Tests { get; set; }
        }

        public sealed class EndToEndTest
        {
            public string GroupName { get; set; }
            public Dictionary<string, string> PossibleExceptions { get; set; }
            public Dictionary<string, string> PossibleExternalMethodSources { get; set; }
            public string Name { get; set; }
            public string Source { get; set; }
            public IJsonObject Transformer { get; set; }
            public IJsonObject? Result { get; set; }
            public string? ExceptionCode { get; set; }
            public string? InnerExceptionCode { get; set; }
            public string? ExceptionType { get; set; }
            public string ExternalMethodSource { get; set; }
        }

        public sealed class JsonTestContainer : TestContainer
        {
            private readonly string _testFileJson;

            public JsonTestContainer(MethodInfo testMethod, JsonTestDefinitionAttribute testAttribute) 
                : base(testMethod, testAttribute)
            {
                _testFileJson = ReadEmbeddedJson(testAttribute.TestResourceName);
                var schemaJson = ReadEmbeddedJson("JsonTest.schema");

                var schema = JsonSchema.FromJsonAsync(schemaJson).Result;

                var evaluationResults = schema.Validate(_testFileJson);

                if (evaluationResults.Count > 0)
                {
                    var resultsText = string.Join(Environment.NewLine, evaluationResults);

                    throw new InvalidDataException(resultsText);
                }
            }

            private string ReadEmbeddedJson(string fileName)
            {
                using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.Resources.TestFiles.JsonTests.{fileName}.json");
                using var reader = new StreamReader(manifestStream);

                return reader.ReadToEnd();
            }

            public override void Execute(IJsonContext context)
            {
                var testFile = context.JsonTokenReader.Read(_testFileJson).ToTypeOf<TestFile>();

                var associatedTests = from testGroup in testFile.TestGroups
                                      from test in testGroup.Tests
                                      select new EndToEndTest
                                      {
                                          InnerExceptionCode = test.InnerExceptionCode,
                                          Name = test.Name,
                                          Result = test.Result,
                                          Transformer = test.Transformer,
                                          ExceptionType = test.ExceptionType,
                                          ExceptionCode = test.ExceptionCode,
                                          GroupName = testGroup.Name,
                                          PossibleExceptions = testFile.PossibleExceptionCodes,
                                          PossibleExternalMethodSources = testFile.PossibleExternalMethodSources,
                                          Source = testGroup.Source.ToString(),
                                          ExternalMethodSource = test.ExternalMethodSource ?? testGroup.ExternalMethodSource
                                      };

                foreach (var test in associatedTests)
                {
                    // We're only working with a single context that's passed in for all tests within the JSON file,
                    // so in case the tests are registering external methods we want to start those fresh each time
                    // to avoid them stacking up and duplicating.

                    context = context.Clear();
                    context.ReferenceResolver.Clear();

                    ExecuteEndToEndTest(test, context);
                }
            }

            private void ExecuteEndToEndTest(EndToEndTest test, IJsonContext context)
            {
                ExceptionCode GetExceptionCodeFrom(string exceptionCodeText)
                {
                    if (!test.PossibleExceptions.TryGetValue(exceptionCodeText, out var exceptionCodeName))
                    {
                        exceptionCodeName = exceptionCodeText;
                    }

                    if (!Enum.TryParse<ExceptionCode>(exceptionCodeName, out var expectedCode))
                    {
                        throw new ArgumentOutOfRangeException(nameof(exceptionCodeText), $"Unable to locate an exception code with the value '{exceptionCodeName}'");
                    }

                    return expectedCode;
                }

                var builtContext = context
                    .UseTransformer(test.Transformer.ToString());

                if (!string.IsNullOrWhiteSpace(test.ExternalMethodSource))
                {
                    if (!test.PossibleExternalMethodSources.TryGetValue(test.ExternalMethodSource, out var externalMethodSource))
                    {
                        externalMethodSource = test.ExternalMethodSource;
                    }

                    var externalMethodType = Type.GetType(externalMethodSource);

                    builtContext = builtContext.RegisterAllMethodsFrom(externalMethodType);

                    var isStaticClass = externalMethodType.IsClass && externalMethodType.IsAbstract && externalMethodType.IsSealed;

                    if (!isStaticClass)
                    {
                        builtContext = builtContext.UseMethodContext(Activator.CreateInstance(externalMethodType));
                    }
                }

                var transformer = new JoltTransformer<IJsonContext>(builtContext);

                try
                {
                    var result = transformer.Transform(test.Source);

                    var resultToken = context.JsonTokenReader.Read(result);
                    var isEqual = test.Result.Equals(resultToken);

                    if (!isEqual)
                    {
                        throw new InvalidOperationException($"Expected JSON result was not present!\nExpected: {test.Result}\nActual: {result}");
                    }
                }
                catch (JoltException ex)
                {
                    if (test.ExceptionCode is null && test.ExceptionType is null)
                    {
                        throw;
                    }

                    if (test.ExceptionCode != null)
                    {
                        // There is a mapping to the actual name of the ExceptionCode value in the JSON test file
                        // and we should default to that if it's present, otherwise we need to assume that they specified
                        // it explicitly and fall back to that if possible.

                        var expectedCode = GetExceptionCodeFrom(test.ExceptionCode);

                        ex.Code.Should().Be(expectedCode, "because this exception code was expected");

                        if (test.InnerExceptionCode != null)
                        {
                            var expectedInnerCode = GetExceptionCodeFrom(test.InnerExceptionCode);

                            if (ex.InnerException is JoltException innerException)
                            {
                                ((JoltException)ex.InnerException).Code.Should().Be(expectedInnerCode, "because this inner exception code was expected");
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException($"Expected inner exception to be of type 'JoltException' and contain code '{expectedInnerCode}' but found exception type '{ex.InnerException.GetType()}' instead");
                            }
                        }
                    }

                    if (test.ExceptionType != null)
                    {
                        ex.GetType().Name.Should().Be(test.ExceptionType, "because this exception type was expected");
                    }
                }
                catch (Exception ex)
                {
                    if (test.ExceptionType is null)
                    {
                        throw;
                    }

                    ex.GetType().Name.Should().Be(test.ExceptionType, "because this exception type was expected");
                }
            }
        }
        
        public JsonTest(Func<IJsonContext> context)
            : base(context)
        {
        }        
    }
}
