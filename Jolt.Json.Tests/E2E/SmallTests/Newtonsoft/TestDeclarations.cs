using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.SmallTests.Newtonsoft;

[Startup(typeof(Startup), Shared = false)]
public sealed class ValueOf([FromKeyedServices(TestType.Newtonsoft)] IJsonContext context)
    : ValueOfTests(context)
{
}
