using Jolt.Json.Tests.Cases.E2E.SmallTests;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.SmallTests.Newtonsoft
{
    public sealed class ValueOf : ValueOfTests
    {
        public sealed class NewtonsoftSmallTestContainer : SmallTestContainer
        {
            public NewtonsoftSmallTestContainer(MethodInfo testMethod, SmallTestDefinitionAttribute testAttribute)
                : base(testMethod, testAttribute)
            {
            }

            public override IJsonContext Context => Startup.CreateNewtonsoftContext();
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(ValueOf), typeof(SmallTestDefinitionAttribute), typeof(NewtonsoftSmallTestContainer))]
        public void ValueOfTests_WillSucceed(NewtonsoftSmallTestContainer container, QuickTest test) => container.Execute(test);
    }
}
