using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.Testing.Harness
{
    /// <summary>
    /// Runs every library reference example in docs/data/methods.yaml that has a source, transformer and expected
    /// output, so the documentation can't silently drift from the engine's behavior.
    /// </summary>
    /// <remarks>
    /// The examples are read from docs/src/generated/doc-examples.json, which is generated (and gitignored), so run
    /// `npm run generate` in docs/ before running these tests.
    /// </remarks>
    public abstract class DocExampleTests
    {
        private const string ExamplesFileName = "doc-examples.json";

        private sealed class DocExample
        {
            public string Method { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public JsonNode? Source { get; set; }

            public JsonNode? Transformer { get; set; }

            public JsonNode? Output { get; set; }
        }

        private static readonly string ExamplesPath = Path.Combine(AppContext.BaseDirectory, ExamplesFileName);

        private static readonly Lazy<IReadOnlyDictionary<string, DocExample>> Examples = new Lazy<IReadOnlyDictionary<string, DocExample>>(Load);

        public static IEnumerable<object[]> ExampleNames() => Examples.Value.Keys.Select(x => new object[] { x });

        /// <summary>
        /// Transforms the source document with the transformer, using the JSON library under test.
        /// </summary>
        protected abstract string? Transform(string transformer, string source);

        [Fact]
        public void DocExamples_AreAvailable()
        {
            Assert.True(File.Exists(ExamplesPath), $"{ExamplesFileName} was not found. Run `npm run generate` in docs/ and rebuild the tests.");
            Assert.NotEmpty(Examples.Value);
        }

        [Theory]
        [MemberData(nameof(ExampleNames))]
        public void DocExample_ProducesDocumentedOutput(string name)
        {
            var example = Examples.Value[name];

            var actual = JsonNode.Parse(Transform(example.Transformer!.ToJsonString(), example.Source!.ToJsonString()) ?? "null");

            Assert.True(
                JsonNode.DeepEquals(example.Output, actual),
                $"{name}{Environment.NewLine}Expected: {example.Output?.ToJsonString() ?? "null"}{Environment.NewLine}Actual:   {actual?.ToJsonString() ?? "null"}");
        }

        private static IReadOnlyDictionary<string, DocExample> Load()
        {
            if (!File.Exists(ExamplesPath))
            {
                return new Dictionary<string, DocExample>();
            }

            var examples = JsonSerializer.Deserialize<List<DocExample>>(File.ReadAllText(ExamplesPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return (examples ?? new List<DocExample>()).ToDictionary(x => $"#{x.Method}: {x.Title}");
        }
    }
}
