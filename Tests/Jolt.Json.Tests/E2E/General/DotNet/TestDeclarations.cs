using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.DependencyInjection;
using static Jolt.Json.Tests.Resources.JsonTest;

namespace Jolt.Json.Tests.E2E.General.DotNet;

[Startup(typeof(Startup), Shared = false)]
public sealed class JoltTransformer([FromKeyedServices(TestType.DotNet)] IJsonContext context)
    : TransformerTests(context)
{
    [Theory]
    [MemberData(nameof(GetAllTestsInScope), typeof(JoltTransformer), typeof(TransformerTestDefinitionAttribute), typeof(TransformerTestContainer))]
    public void TransformerTests_WillSucceed(TransformerTestContainer container) => container.Execute(_testContext, this);
}
