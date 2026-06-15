using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Assertions.Exceptions
{
    internal sealed class NullExpectationFailedException : ExpectationFailedException
    {
        public NullExpectationFailedException(string because) : base("Expected null value", $"{because} but found a non-null value") { }
    }
}
