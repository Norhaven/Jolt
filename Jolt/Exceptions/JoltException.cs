using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public abstract class JoltException : Exception
    {
        public ExceptionCode Code { get; }

        public JoltException(ExceptionCode code, string message, JoltException? innerException = default)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}
