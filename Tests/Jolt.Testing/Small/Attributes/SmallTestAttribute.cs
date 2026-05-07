using Jolt.Testing.Attributes;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Jolt.Testing.Small.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [XunitTestCaseDiscoverer("Jolt.Testing.Small.SmallTestDiscoverer", "Jolt.Testing")]
    public sealed class SmallTestAttribute : TestDiscoveryAttribute
    {
        public Type TestContainerType { get; }

        public SmallTestAttribute(Type testContextType, TestType testType, Type testContainerType)
            : base(testContextType, testType)
        {
            Trace.WriteLine($"Initializing SmallTestAttribute with contextFactoryType='{testContextType.FullName}', testType='{testType}'");

            TestContainerType = testContainerType ?? throw new ArgumentNullException(nameof(testContainerType));
        }
    }
}
