using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources.TestAttributes;
using NJsonSchema;
using NJsonSchema.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.Json.Tests.Resources
{
    public abstract class JsonTest : Test
    {
        public sealed class TestFile
        {
            public JsonNode PossibleExceptionCodes { get; set; }
            public JsonNode PossibleExternalMethodSources { get; set; }
            public TestGroup[] TestGroups { get; set; }
        }

        public sealed class TestGroup
        {
            public string Name { get; set; }
            public JsonNode Source { get; set; }
            public string ExternalMethodSource { get; set; }
            public EndToEndTest[] Tests { get; set; }
        }

        public sealed class EndToEndTest
        {
            public string GroupName { get; set; }
            public IDictionary<string, string> PossibleExceptions { get; set; }
            public IDictionary<string, string> PossibleExternalMethodSources { get; set; }
            public string Name { get; set; }
            public string Source { get; set; }
            public JsonNode Transformer { get; set; }
            public JsonNode? Result { get; set; }
            public string? ExceptionCode { get; set; }
            public string? InnerExceptionCode { get; set; }
            public string? ExceptionType { get; set; }
            public string ExternalMethodSource { get; set; }
        }

        public sealed class JsonTestContainer : TestContainer
        {
            private readonly TestFile _testFile;

            public JsonTestContainer(MethodInfo testMethod, JsonTestDefinitionAttribute testAttribute) 
                : base(testMethod, testAttribute)
            {
                var json = ReadEmbeddedJson(testAttribute.TestResourceName);
                var schemaJson = ReadEmbeddedJson("JsonTestSchema");

                var schema = JsonSchema.FromJsonAsync(schemaJson).Result;
                var jsonElement = JsonElement.Parse(json);

                var evaluationResults = schema.Validate(json);

                if (evaluationResults.Count > 0)
                {
                    var resultsText = string.Join(Environment.NewLine, evaluationResults);

                    throw new InvalidDataException(resultsText);
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                _testFile = JsonSerializer.Deserialize<TestFile>(json, options);
            }

            private string ReadEmbeddedJson(string fileName)
            {
                using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.Resources.TestFiles.JsonTests.{fileName}.json");
                using var reader = new StreamReader(manifestStream);

                return reader.ReadToEnd();
            }

            public override void Execute(IJsonContext context)
            {
                var possibleExceptions = _testFile.PossibleExceptionCodes.Deserialize<Dictionary<string, string>>();
                var possibleExternalMethodSources = _testFile.PossibleExternalMethodSources.Deserialize<Dictionary<string, string>>();

                var associatedTests = from testGroup in _testFile.TestGroups
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
                                          PossibleExceptions = possibleExceptions,
                                          PossibleExternalMethodSources = possibleExternalMethodSources,
                                          Source = testGroup.Source.ToJsonString(),
                                          ExternalMethodSource = test.ExternalMethodSource ?? testGroup.ExternalMethodSource
                                      };

                foreach (var test in associatedTests)
                {
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
                    .UseTransformer(test.Transformer.ToJsonString());

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

                    result.Should().Be(test.Result.ToJsonString(new JsonSerializerOptions() { WriteIndented = false }), "because the result should exactly match the specified output");
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
        
        public JsonTest(IJsonContext context)
            : base(context)
        {
        }        
    }
}
