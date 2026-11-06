using Jolt.Json.DotNet;

namespace Jolt.Testing.Harness.DotNet
{
    public class DocExampleTests : Harness.DocExampleTests
    {
        protected override string? Transform(string transformer, string source) =>
            JoltJsonTransformer.DefaultWith<DocExampleTests>(transformer).Transform(source);
    }
}
