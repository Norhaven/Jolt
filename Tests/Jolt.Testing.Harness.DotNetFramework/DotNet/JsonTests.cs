using Jolt.Testing.Json;
using Jolt.Testing.Json.Attributes;

namespace Jolt.Testing.Harness.DotNetFramework.DotNet
{
    public class JsonTests : TestContainer
    {
        [JsonTest("IndexingWithVariables", typeof(TestContext), TestType.DotNet)]
        public void IndexingWithVariables(EndToEndTest test) { Execute(test); }

        [JsonTest("LibraryMethodsWithVariables", typeof(TestContext), TestType.DotNet)]
        public void LibraryMethodsWithVariables(EndToEndTest test) { Execute(test); }

        [JsonTest("ExternalMethods", typeof(TestContext), TestType.DotNet)]
        public void ExternalMethods(EndToEndTest test) { Execute(test); }

        [JsonTest("OperatorsAndVariables", typeof(TestContext), TestType.DotNet)]
        public void OperatorsAndVariables(EndToEndTest test) { Execute(test); }

        [JsonTest("Literals", typeof(TestContext), TestType.DotNet)]
        public void Literals(EndToEndTest test) { Execute(test); }

        [JsonTest("MappingWithVariables", typeof(TestContext), TestType.DotNet)]
        public void MappingWithVariables(EndToEndTest test) { Execute(test); }
    }
}
