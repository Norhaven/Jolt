using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public class JoltParsingException : Exception
    {
        public JoltParsingException(string message)
            : base(message) { }
    }
}
