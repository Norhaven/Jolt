using Jolt.Testing.Small;
using Jolt.Testing.Small.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness.DotNetFramework.DotNet
{
    public sealed class SmallTests : StandardValueOfTests
    {
        [SmallTest(typeof(TestContext), TestType.DotNet, typeof(SmallTests))]
        public void ValueOfTests(SmallTest test) { Execute(test); }
    }
}
