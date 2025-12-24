using Jolt.Json.Tests.Cases.E2E.JsonTests;
using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jolt.Json.DotNetFramework.Tests;

namespace Jolt.Json.DotNetFramework.Tests.E2E.JsonTests.DotNet
{
    public sealed class JsonTestHarness : JsonTest
    {
        public JsonTestHarness()
            :base(Startup.CreateDotNetContext())
        {

        }
    }
}

