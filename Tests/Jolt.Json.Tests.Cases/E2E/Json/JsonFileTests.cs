using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Cases.E2E.Json
{
    public abstract class JsonFileTests : JsonTest
    {
        protected JsonFileTests(Func<IJsonContext> context) 
            : base(context)
        {
        }

        [JsonTestDefinition("ExternalMethodsTests")]
        public void ExternalMethodsTests() { }

        [JsonTestDefinition("OperatorsAndVariablesTests")]
        public void OperatorsAndVariablesTests_WillSucceed() { }
    }
}
