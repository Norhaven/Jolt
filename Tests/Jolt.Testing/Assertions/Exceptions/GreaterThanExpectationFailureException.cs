using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Assertions.Exceptions
{
    internal sealed class GreaterThanExpectationFailureException : ExpectationFailedException
    {
        public GreaterThanExpectationFailureException(object value, string reason) : base($"Expected value to be greater than '{value}'", $"{reason} but found a value that was less than or equal") { }
    }
}
