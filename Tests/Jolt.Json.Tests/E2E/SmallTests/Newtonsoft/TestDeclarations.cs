using Jolt.Json.Tests.Cases.E2E.SmallTests;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
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
    [Theory]
    [MemberData(nameof(GetAllTestsInScope), typeof(ValueOf), typeof(SmallTestDefinitionAttribute), typeof(SmallTestContainer))]
    public void ValueOfTests_WillSucceed(SmallTestContainer container) => container.Execute(_testContext);
}
