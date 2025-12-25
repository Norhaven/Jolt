using Jolt.Json.Tests.Cases.E2E.Json;
using Jolt.Json.Tests.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.DependencyInjection;
using static Jolt.Json.Tests.Resources.SmallTest;

namespace Jolt.Json.Tests.E2E.JsonTests.DotNet;

[Startup(typeof(Startup), Shared = false)]
public sealed class JsonTestHarness([FromKeyedServices(TestType.DotNet)] IJsonContext context)
    : JsonFileTests(context)
{
    [Theory]
    [MemberData(nameof(GetAllTestsInScope), typeof(JsonTestHarness), typeof(JsonTestDefinitionAttribute), typeof(JsonTestContainer))]
    public void JsonTests_WillSucceed(JsonTestContainer container) => container.Execute(_testContext);
}

