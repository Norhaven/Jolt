using Jolt.Testing.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Jolt.Testing.Json.Attributes
{
    /// <summary>
    /// Attribute for test methods that should discover and run individual JSON test cases.
    /// Each test in the JSON file will appear as a distinct entry in Test Explorer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Jolt.Testing.Json.JsonTestDiscoverer", "Jolt.Testing")]
    public sealed class JsonTestAttribute : TestDiscoveryAttribute
    {
        /// <summary>
        /// The name of the JSON test file (without extension) from TestFiles\JsonTests directory.
        /// </summary>
        public string TestJsonFile { get; }

        public JsonTestAttribute(string testJsonFile, Type testContextType, TestType testType)
            : base(testContextType, testType)
        {   
            Trace.WriteLine($"Initializing JsonTestAttribute with testJsonFile='{testJsonFile}', testContextType='{testContextType.FullName}', testType='{testType}'");

            TestJsonFile = "JsonTests." + testJsonFile ?? throw new ArgumentNullException(nameof(testJsonFile));         
        }
    }
}
