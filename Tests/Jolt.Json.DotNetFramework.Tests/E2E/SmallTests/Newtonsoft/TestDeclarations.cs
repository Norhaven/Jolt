using Jolt.Json.Tests.Cases.E2E.SmallTests;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.SmallTests.Newtonsoft
{
    public sealed class ValueOf : ValueOfTests
    {
        public ValueOf()
            :base(Startup.CreateNewtonsoftContext)
        {
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(ValueOf), typeof(SmallTestDefinitionAttribute), typeof(SmallTestContainer))]
        public void ValueOfTests_WillSucceed(SmallTestContainer container) => container.Execute(Context);
    }
}
