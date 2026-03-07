using Jolt.Json.Tests.Cases.E2E.SmallTests;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.DependencyInjection;
using static Jolt.Json.Tests.Resources.JsonTest;

namespace Jolt.Json.Tests.E2E.SmallTests.DotNet;

public sealed class ValueOf
    : ValueOfTests
{
    public sealed class DotNetSmallTestContainer : SmallTestContainer
    {
        public DotNetSmallTestContainer(MethodInfo testMethod, SmallTestDefinitionAttribute testAttribute)
            : base(testMethod, testAttribute)
        {
        }

        public override IJsonContext Context => Startup.CreateDotNetContext();
    }

    [Theory]
    [MemberData(nameof(GetAllTestsInScope), typeof(ValueOf), typeof(SmallTestDefinitionAttribute), typeof(DotNetSmallTestContainer))]
    public void ValueOfTests_WillSucceed(DotNetSmallTestContainer container, QuickTest test) => container.Execute(test);
}

