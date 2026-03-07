using System;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    /// <summary>
    /// Marks a test method as a JSON test that should be discovered and executed.
    /// The test method should accept three parameters: (string testGroup, string testName, string jsonFileName).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class JsonTestDefinitionAttribute : Attribute
    {
        public string TestResourceName { get; }

        public JsonTestDefinitionAttribute(string testResourceName)
        {
            TestResourceName = testResourceName;
        }
    }
}
