using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Assertions.Exceptions
{
    internal sealed class NonNullExpectationFailedException : ExpectationFailedException
    {
        public NonNullExpectationFailedException(string because) : base("Expected non-null value", $"{because} but found a null value") { }
    }
}
