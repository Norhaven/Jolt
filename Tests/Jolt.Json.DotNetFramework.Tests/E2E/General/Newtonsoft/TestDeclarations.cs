using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.General.Newtonsoft
{
    public sealed class JoltTransformer : TransformerTests
    {
        public JoltTransformer()
            : base(Startup.CreateNewtonsoftContext())
        {
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(JoltTransformer), typeof(TransformerTestDefinitionAttribute), typeof(TransformerTestContainer))]
        public void TransformerTests_WillSucceed(TransformerTestContainer container) => container.Execute(_testContext, this);
    }
}
