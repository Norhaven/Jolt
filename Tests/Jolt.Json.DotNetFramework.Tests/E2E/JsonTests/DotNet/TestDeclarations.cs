using Jolt.Json.DotNetFramework.Tests;
using Jolt.Json.Tests.Cases.E2E.General;
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

namespace Jolt.Json.DotNetFramework.Tests.E2E.JsonTests.DotNet
{
    public sealed class JsonTestHarness : JsonFileTests
    {
        public sealed class DotNetJsonTestContainer : JsonTestContainer
        {
            public DotNetJsonTestContainer(MethodInfo testMethod, JsonTestDefinitionAttribute testAttribute)
                : base(testMethod, testAttribute)
            {
            }

            public override IJsonContext Context => Startup.CreateDotNetContext();
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(JsonTestHarness), typeof(JsonTestDefinitionAttribute), typeof(DotNetJsonTestContainer))]
        public void JsonTests_WillSucceed(DotNetJsonTestContainer container, EndToEndTest test) => container.Execute(test);
    }
}

