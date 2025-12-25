using Jolt.Json.Tests.Cases.E2E.Json;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.JsonTests.Newtonsoft;

[Startup(typeof(Startup), Shared = false)]
public sealed class JsonTestHarness([FromKeyedServices(TestType.Newtonsoft)] IJsonContext context)
    : JsonFileTests(context)
{
    [Theory]
    [MemberData(nameof(GetAllTestsInScope), typeof(JsonTestHarness), typeof(JsonTestDefinitionAttribute), typeof(JsonTestContainer))]
    public void JsonTests_WillSucceed(JsonTestContainer container) => container.Execute(_testContext);
}
