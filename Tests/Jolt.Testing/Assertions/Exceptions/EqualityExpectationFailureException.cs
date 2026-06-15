using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Assertions.Exceptions
{
    internal sealed class EqualityExpectationFailureException : ExpectationFailedException
    {
        public EqualityExpectationFailureException(object value, string reason) : base($"Expected value to be equal to '{value}'", $"{reason} but found a different value") { }
    }
}
