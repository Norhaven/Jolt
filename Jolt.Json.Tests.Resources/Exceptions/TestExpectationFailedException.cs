using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests.Resources.Exceptions
{
    public sealed class TestExpectationFailedException : Exception
    {
        public TestExpectationFailedException(string message) : base(message)
        {
        }
    }
}
