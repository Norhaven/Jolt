using Jolt.Json.Tests.Cases.E2E.Json;
using Jolt.Json.Tests.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.JsonTests.DotNet;

public sealed class JsonTestHarness
    : JsonFileTests
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

