using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.General.DotNet
{
    public sealed class JoltTransformer : TransformerTests
    {
        public sealed class DotNetTransformerTestContainer : TransformerTestContainer
        {
            public DotNetTransformerTestContainer(MethodInfo testMethod, TransformerTestDefinitionAttribute testAttribute)
                : base(testMethod, testAttribute)
            {
            }

            public override IJsonContext Context => Startup.CreateDotNetContext();
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(JoltTransformer), typeof(TransformerTestDefinitionAttribute), typeof(DotNetTransformerTestContainer))]
        public void TransformerTests_WillSucceed(DotNetTransformerTestContainer container) => container.Execute(this);
    }
}
