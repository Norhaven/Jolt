using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Json
{
    public interface ITestContext
    {
        IJsonContext CreateJsonContext(TestType testType);
        IJsonObject ParseAsJsonObject(TestType testType, string json);
    }
}
