using Jolt.LanguageServer.Preview;
using Jolt.LanguageServer.Validation;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Jolt.LanguageServer.Tests
{
    public sealed class TransformRunnerTests
    {
        private static TransformResult Run(string transformer, string inputFile, DocumentContext? context = null, ValidationSettings? settings = null) =>
            TransformRunner.Run(transformer, PreviewInput.Parse(inputFile), context ?? DocumentContext.Unknown, settings ?? ValidationSettings.Default);

        private static JsonElement Output(TransformResult result)
        {
            Assert.True(result.Outcome == TransformOutcome.Success, result.Error?.Message);

            return JsonDocument.Parse(result.Output!).RootElement;
        }

        [Fact]
        public void Run_TransformsTheInputDocument()
        {
            var result = Run("{ \"name\": \"#valueOf($.customer.name)\" }", "{ \"input\": { \"customer\": { \"name\": \"Ada\" } } }");

            Assert.Equal("Ada", Output(result).GetProperty("name").GetString());
            Assert.NotNull(result.ElapsedMilliseconds);
        }

        [Fact]
        public void Run_WithMock_ReturnsTheMockedValueForEachCall()
        {
            const string transformer = "{ \"a\": \"#lookupRegion($.country)\", \"b\": \"#lookupRegion('FR')\", \"tags\": \"#tagsFor($.items, @x: @x.id)\" }";
            const string inputFile = "{ \"input\": { \"country\": \"DE\", \"items\": [] }, \"mocks\": { \"lookupRegion\": { \"returns\": \"EMEA\" }, \"tagsFor\": { \"returns\": [\"a\", \"b\"] } } }";

            var output = Output(Run(transformer, inputFile));

            Assert.Equal("EMEA", output.GetProperty("a").GetString());
            Assert.Equal("EMEA", output.GetProperty("b").GetString());
            Assert.Equal(2, output.GetProperty("tags").GetArrayLength());
        }

        [Fact]
        public void Run_WithMockForUnlistedMethod_ValidatesAsKnown()
        {
            // The methods.json files don't list the mocked method, which would otherwise be reported as unknown.
            var context = new DocumentContext(new[] { "other" }, null);

            var output = Output(Run("{ \"a\": \"#lookupRegion()\" }", "{ \"input\": {}, \"mocks\": { \"lookupRegion\": { \"returns\": 1 } } }", context));

            Assert.Equal(1, output.GetProperty("a").GetInt32());
        }

        [Fact]
        public void Run_WithKnownCustomMethodWithoutMock_ReportsTheMissingMock()
        {
            var context = new DocumentContext(new[] { "lookupRegion" }, null);

            var result = Run("{ \"a\": \"#lookupRegion($.country)\" }", "{ \"input\": { \"country\": \"DE\" } }", context);

            Assert.Equal(TransformOutcome.Error, result.Outcome);
            Assert.Contains(new[] { result.Error! }.Concat(result.Error!.InnerErrors), x => x.Message.Contains("'#lookupRegion' has no mock"));
        }

        [Fact]
        public void Run_WithMockForStandardLibraryMethod_WarnsThatItIsIgnored()
        {
            var result = Run("{ \"a\": \"#valueOf($.a)\" }", "{ \"input\": { \"a\": 1 }, \"mocks\": { \"valueOf\": { \"returns\": 2 } } }");

            Assert.Equal(1, Output(result).GetProperty("a").GetInt32());
            Assert.Contains("'valueOf' is ignored", Assert.Single(result.Warnings));
        }

        [Fact]
        public void Run_WithTransformerErrors_IsSkipped()
        {
            var result = Run("{ \"a\": \"@missing + @alsoMissing\" }", "{ \"input\": {} }");

            Assert.Equal(TransformOutcome.Skipped, result.Outcome);
            Assert.Equal(2, result.TransformerErrorCount);
            Assert.Null(result.Output);
        }

        [Fact]
        public void Run_WithRuntimeError_ReportsTheErrorAndItsCode()
        {
            var result = Run("{ \"a\": \"#toInteger($.a)\" }", "{ \"input\": { \"a\": \"not a number\" } }");

            Assert.Equal(TransformOutcome.Error, result.Outcome);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Run_WithTestDocument_IsNotSupported()
        {
            var result = Run("{ \"testGroups\": [] }", "{ \"input\": {} }");

            Assert.Equal(TransformOutcome.Error, result.Outcome);
            Assert.Contains("test files", result.Error!.Message);
        }

        [Theory]
        [InlineData("{ \"input\": ", "isn't valid JSON (line 1")]
        [InlineData("[]", "must be an object")]
        [InlineData("{ \"mocks\": {} }", "no \"input\" property")]
        [InlineData("{ \"input\": {}, \"mocks\": [] }", "\"mocks\" property must be an object")]
        [InlineData("{ \"input\": {}, \"mocks\": { \"a\": true } }", "mock for 'a' must be an object with a \"returns\" property")]
        public void Run_WithInvalidInputFile_ReportsWhy(string inputFile, string expected)
        {
            var result = Run("{ \"a\": 1 }", inputFile);

            Assert.Equal(TransformOutcome.InvalidInput, result.Outcome);
            Assert.Contains(expected, result.Error!.Message);
        }

        [Fact]
        public void Run_WithUnsafeMethod_FollowsTheSetting()
        {
            const string transformer = "{ \"a\": \"#eval('1 + 1')\" }";

            // Validation reports an unsafe method as a warning, so it runs, and Jolt refuses to call it.
            Assert.Equal(TransformOutcome.Error, Run(transformer, "{ \"input\": {} }").Outcome);
            Assert.Equal(2, Output(Run(transformer, "{ \"input\": {} }", settings: new ValidationSettings(true))).GetProperty("a").GetInt32());
        }
    }
}
