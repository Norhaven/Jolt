using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public abstract class JoltException : Exception
    {
        public JoltException(string message)
            : base(message)
        {
        }
    }
}
