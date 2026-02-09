using Jolt.Json.Tests.Cases.E2E.Json;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.JsonTests.Newtonsoft
{
    public sealed class JsonTestHarness : JsonFileTests
    {
        public JsonTestHarness()
            : base(Startup.CreateNewtonsoftContext)
        {
        }

        [Theory]
        [MemberData(nameof(GetAllTestsInScope), typeof(JsonTestHarness), typeof(JsonTestDefinitionAttribute), typeof(JsonTestContainer))]
        public void JsonTests_WillSucceed(JsonTestContainer container) => container.Execute(Context);
    }
}
