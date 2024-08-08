using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public abstract class JoltException : Exception
    {
        public ExceptionCode Code { get; }

        public JoltException(ExceptionCode code, string message)
            : base(message)
        {
            Code = code;
        }
    }
}
