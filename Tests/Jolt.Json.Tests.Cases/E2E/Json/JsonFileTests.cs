using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;

namespace Jolt.Json.Tests.Cases.E2E.Json
{
    public abstract class JsonFileTests : JsonTest
    {
        [JsonTestDefinition("IndexingWithVariables")]
        public void IndexingWithVariablesTests() { }

        [JsonTestDefinition("LibraryMethodsWithVariables")]
        public void LibraryMethodsWithVariablesTests() { }

        [JsonTestDefinition("ExternalMethodsTests")]
        public void ExternalMethodsTests() { }

        [JsonTestDefinition("OperatorsAndVariablesTests")]
        public void OperatorsAndVariablesTests_WillSucceed() { }
    }
}
