using Jolt.Json.Tests.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.DotNetFramework.Tests.E2E.JsonTests.Newtonsoft
{
    public sealed class JsonTestHarness : JsonTest
    {
        public JsonTestHarness()
            : base(Startup.CreateNewtonsoftContext())
        {

        }
    }
}
