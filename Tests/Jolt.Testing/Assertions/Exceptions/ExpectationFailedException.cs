using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Assertions.Exceptions
{
    internal abstract class ExpectationFailedException : Exception
    {
        protected ExpectationFailedException(string message, string reason) : base($"{message} {reason}") { }
    }
}
