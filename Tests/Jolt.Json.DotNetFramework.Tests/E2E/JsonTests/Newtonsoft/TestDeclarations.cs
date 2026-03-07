using Jolt.Json.Tests.Cases.E2E.Json;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Jolt.Json.Tests.Resources.TransformerTest;

namespace Jolt.Json.DotNetFramework.Tests.E2E.JsonTests.Newtonsoft
{
    public sealed class JsonTestHarness : JsonFileTests
    {
        public sealed class NewtonsoftJsonTestContainer : JsonTestContainer
        {
            public NewtonsoftJsonTestContainer(MethodInfo testMethod, JsonTestDefinitionAttribute testAttribute)
                : base(testMethod, testAttribute)
            {
            }

            public override IJsonContext Context => Startup.CreateNewtonsoftContext();
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(JsonTestHarness), typeof(JsonTestDefinitionAttribute), typeof(NewtonsoftJsonTestContainer))]
        public void JsonTests_WillSucceed(NewtonsoftJsonTestContainer container, EndToEndTest test) => container.Execute(test);
    }
}
