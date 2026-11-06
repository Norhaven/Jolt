using Jolt.Json.Newtonsoft;

namespace Jolt.Testing.Harness.Newtonsoft
{
    public class DocExampleTests : Harness.DocExampleTests
    {
        protected override string? Transform(string transformer, string source) =>
            JoltJsonTransformer.DefaultWith<DocExampleTests>(transformer).Transform(source);
    }
}
