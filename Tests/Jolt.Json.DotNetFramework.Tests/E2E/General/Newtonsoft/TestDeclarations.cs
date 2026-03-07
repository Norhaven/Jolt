using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.General.Newtonsoft
{
    public sealed class JoltTransformer : TransformerTests
    {
        public sealed class NewtonsoftTransformerTestContainer : TransformerTestContainer
        {
            public NewtonsoftTransformerTestContainer(MethodInfo testMethod, TransformerTestDefinitionAttribute testAttribute)
                : base(testMethod, testAttribute)
            {
            }

            public override IJsonContext Context => Startup.CreateNewtonsoftContext();
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(JoltTransformer), typeof(TransformerTestDefinitionAttribute), typeof(NewtonsoftTransformerTestContainer))]
        public void TransformerTests_WillSucceed(NewtonsoftTransformerTestContainer container) => container.Execute(this);
    }
}
