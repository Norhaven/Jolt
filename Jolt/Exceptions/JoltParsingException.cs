using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public sealed class JoltParsingException : Exception
    {
        public JoltParsingException(string message)
            : base(message) { }
    }
}
