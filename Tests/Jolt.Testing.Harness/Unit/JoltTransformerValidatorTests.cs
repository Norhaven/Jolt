using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness.Unit
{
    public sealed class JoltTransformerValidatorTests : Testing.Unit.JoltTransformerValidatorTests
    {
        protected override IJsonContext CreateContext(TestType testType) => new TestContext().CreateJsonContext(testType);
    }
}
