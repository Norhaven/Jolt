using Jolt.Testing.Small;
using Jolt.Testing.Small.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness.Newtonsoft
{
    public sealed class SmallTests : StandardValueOfTests
    {
        [SmallTest(typeof(TestContext), TestType.Newtonsoft, typeof(SmallTests))]
        public void ValueOfTests(SmallTest test) { Execute(test); }
    }
}
