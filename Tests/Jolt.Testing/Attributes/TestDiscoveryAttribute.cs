using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace Jolt.Testing.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class TestDiscoveryAttribute : FactAttribute
    {
        public TestType TestType { get; }

        public ITestContext TestContext { get; }

        public TestDiscoveryAttribute(Type testContextType, TestType testType)
        {
            Trace.WriteLine($"Initializing JsonTestAttribute with testContextType ='{testContextType.FullName}', testType ='{testType}'");

            TestType = testType;
            TestContext = Activator.CreateInstance(testContextType) as ITestContext;
        }
    }
}
