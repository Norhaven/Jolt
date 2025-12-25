using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.General.Newtonsoft;

[Startup(typeof(Startup), Shared = false)]
public sealed class JoltTransformer([FromKeyedServices(TestType.Newtonsoft)] IJsonContext context)
    : TransformerTests(context)
{
    [Theory]
    [MemberData(nameof(GetAllTestsInScope), typeof(JoltTransformer), typeof(TransformerTestDefinitionAttribute), typeof(TransformerTestContainer))]
    public void TransformerTests_WillSucceed(TransformerTestContainer container) => container.Execute(_testContext, this);
}
