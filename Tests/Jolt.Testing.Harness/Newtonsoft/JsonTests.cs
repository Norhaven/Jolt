using Jolt.Testing.Json;
using Jolt.Testing.Json.Attributes;

namespace Jolt.Testing.Harness.Newtonsoft
{
    public class JsonTests : TestContainer
    {
        [JsonTest("IndexingWithVariables", typeof(TestContext), TestType.Newtonsoft)]
        public void IndexingWithVariables(EndToEndTest test) { Execute(test); }

        [JsonTest("LibraryMethodsWithVariables", typeof(TestContext), TestType.Newtonsoft)]
        public void LibraryMethodsWithVariables(EndToEndTest test) { Execute(test); }

        [JsonTest("ExternalMethods", typeof(TestContext), TestType.Newtonsoft)]
        public void ExternalMethods(EndToEndTest test) { Execute(test); }

        [JsonTest("OperatorsAndVariables", typeof(TestContext), TestType.Newtonsoft)]
        public void OperatorsAndVariables(EndToEndTest test) { Execute(test); }
    }
}
