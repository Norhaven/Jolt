using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public sealed class JoltExecutionException : JoltException
    {
        public JoltExecutionException(ExceptionCode code, string message, JoltException? innerException = default) 
            : base(code, message, innerException)
        {
        }
    }
}
