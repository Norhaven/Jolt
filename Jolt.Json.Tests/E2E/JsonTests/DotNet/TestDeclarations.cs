using Jolt.Json.Tests.E2E.General;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Jolt.Json.Tests.Test;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.JsonTests.DotNet;

[Startup(typeof(Startup), Shared = false)]
public sealed class JsonTestHarness([FromKeyedServices(TestType.DotNet)] IJsonContext context)
    : JsonTest(context)
{
}

