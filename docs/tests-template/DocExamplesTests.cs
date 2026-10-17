// Runs every library-reference example that has a source, transformer and expected output
// against the real Jolt engine, so documentation can't silently drift from behavior.
//
// Setup:
//   1. Copy this file into a test project that references Jolt.Json.Newtonsoft and xunit.
//   2. Make docs/src/generated/doc-examples.json available to the tests, for example by adding
//      to the test .csproj:
//        <None Include="..\..\docs\src\generated\doc-examples.json" Link="doc-examples.json"
//              CopyToOutputDirectory="PreserveNewest" />
//   3. Run `npm run generate` in docs/ first (CI: run it before `dotnet test`).
//
// Adjust the namespace and transformer construction to match your test project.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jolt.Json.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jolt.Tests.Documentation;

public sealed class DocExamplesTests
{
    private sealed class DocExample
    {
        public string Method { get; set; } = "";
        public string Title { get; set; } = "";
        public JToken Source { get; set; } = JValue.CreateNull();
        public JToken Transformer { get; set; } = JValue.CreateNull();
        public JToken Output { get; set; } = JValue.CreateNull();
    }

    private static readonly IReadOnlyDictionary<string, DocExample> Examples = Load();

    private static IReadOnlyDictionary<string, DocExample> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "doc-examples.json");
        var examples = JsonConvert.DeserializeObject<List<DocExample>>(File.ReadAllText(path)) ?? new();
        return examples.ToDictionary(e => $"#{e.Method}: {e.Title}");
    }

    public static IEnumerable<object[]> ExampleNames() => Examples.Keys.Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(ExampleNames))]
    public void DocumentedExampleProducesDocumentedOutput(string name)
    {
        var example = Examples[name];

        var transformer = JoltJsonTransformer.DefaultWith<DocExamplesTests>(example.Transformer.ToString());
        var actual = JToken.Parse(transformer.Transform(example.Source.ToString()));

        Assert.True(
            JToken.DeepEquals(example.Output, actual),
            $"{name}\nExpected: {example.Output.ToString(Formatting.None)}\nActual:   {actual.ToString(Formatting.None)}");
    }
}
