using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.Tests.Cases.E2E.JsonTests
{
    public abstract class JsonTest : Test
    {
        public sealed class TestFile
        {
            public JsonNode PossibleExceptionCodes { get; set; }
            public TestGroup[] TestGroups { get; set; }
        }

        public sealed class TestGroup
        {
            public string Name { get; set; }
            public JsonNode Source { get; set; }
            public EndToEndTest[] Tests { get; set; }
        }

        public sealed class EndToEndTest
        {
            public string GroupName { get; set; }
            public IDictionary<string, string> PossibleExceptions { get; set; }
            public string Name { get; set; }
            public string Source { get; set; }
            public JsonNode Transformer { get; set; }
            public JsonNode? Result { get; set; }
            public string? ExceptionCode { get; set; }
            public string? InnerExceptionCode { get; set; }
            public string? ExceptionType { get; set; }
        }

        public JsonTest(IJsonContext context)
            :base(context)
        {

        }

        [Theory]
        [MemberData(nameof(GetTests))]
        public void ExecuteEndToEndTest(EndToEndTest test)
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

            var context = _testContext
                .UseTransformer(test.Transformer.ToJsonString());

            var transformer = new JoltTransformer<IJsonContext>(context);

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

        public static TheoryData<EndToEndTest> GetTests()
        {
            using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.Cases.E2E.JsonTests.Tests.json");
            using var reader = new StreamReader(manifestStream);

            var json = reader.ReadToEnd();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var testFile = JsonSerializer.Deserialize<TestFile>(json, options);
            var possibleExceptions = testFile.PossibleExceptionCodes.Deserialize<Dictionary<string, string>>();

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
                                      PossibleExceptions = possibleExceptions,
                                      Source = testGroup.Source.ToJsonString()
                                  };

            return new TheoryData<EndToEndTest>(associatedTests);
        }
    }
}
