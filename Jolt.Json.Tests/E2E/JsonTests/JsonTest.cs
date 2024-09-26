using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.E2E.JsonTests;

public abstract class JsonTest(IJsonContext context): Test(context)
{
    public sealed record TestFile(JsonNode PossibleExceptionCodes, TestGroup[] TestGroups);
    public sealed record TestGroup(string Name, JsonNode Source, EndToEndTest[] Tests);
    public sealed record EndToEndTest(string GroupName, IDictionary<string, string> PossibleExceptions, string Name, string Source, JsonNode Transformer, JsonNode? Result, string? ExceptionCode, string? ExceptionType);

    [Theory]
    [MemberData(nameof(GetTests))]
    public void ExecuteTest(EndToEndTest test)
    {        
        var transformer = CreateTransformerWith(test.Transformer.ToJsonString(), []);

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

            if (test.ExceptionCode is not null)
            {
                // There is a mapping to the actual name of the ExceptionCode value in the JSON test file
                // and we should default to that if it's present, otherwise we need to assume that they specified
                // it explicitly and fall back to that if possible.

                if (!test.PossibleExceptions.TryGetValue(test.ExceptionCode, out var exceptionCodeName))
                {
                    exceptionCodeName = test.ExceptionCode;
                }

                if (!Enum.TryParse<ExceptionCode>(exceptionCodeName, out var expectedCode))
                {
                    throw new ArgumentOutOfRangeException(nameof(test.ExceptionCode), $"Unable to locate an exception code with the value '{exceptionCodeName}'");
                }

                ex.Code.Should().Be(expectedCode, "because this exception code was expected");
            }

            if (test.ExceptionType is not null)
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
        string? StringFor(IJsonToken token, string propertyName) => token.AsObject()[propertyName]?.ToTypeOf<string>();

        using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.E2E.JsonTests.Tests.json");
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
                              select test with 
                              { 
                                  GroupName = testGroup.Name,
                                  PossibleExceptions = possibleExceptions,
                                  Source = testGroup.Source.ToJsonString() 
                              };

        return new TheoryData<EndToEndTest>(associatedTests);
    }
}
