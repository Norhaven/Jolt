using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.JsonTests.Newtonsoft;

[Startup(typeof(Startup), Shared = false)]
public sealed class JsonTestHarness([FromKeyedServices(TestType.Newtonsoft)] IJsonContext context)
    : JsonTest(context)
{
}
