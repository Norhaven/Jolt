using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    public sealed class JsonTestDefinitionAttribute : Attribute
    {
        public string TestResourceName { get; }

        public JsonTestDefinitionAttribute(string testResourceName)
        {
            TestResourceName = testResourceName;
        }
    }
}
