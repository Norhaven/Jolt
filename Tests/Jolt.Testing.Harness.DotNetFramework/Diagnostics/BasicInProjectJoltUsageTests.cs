using Jolt.Json.DotNet;
using Jolt.Testing.Assertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Testing.Harness.DotNetFramework.Diagnostics
{
    public sealed class BasicInProjectJoltUsageTests
    {
        [Fact]
        public void ValueOf_ShouldBeSuccessful()
        {
            var source = @"{ ""value"": 42 }";
            var transformer = @"{ ""transformedValue"": ""#valueOf($.value)"" }";

            var jsonTransformer = JoltJsonTransformer.DefaultWith(transformer, default);
            var result = jsonTransformer.Transform(source);

            result.Should().NotBeNullOrWhiteSpace("because the transformation should have produced a result");
        }
    }
}
