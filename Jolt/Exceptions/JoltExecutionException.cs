using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Represents a specific exception that occurred during the transformation process.
    /// </summary>
    public sealed class JoltExecutionException : JoltException
    {
        /// <summary>
        /// Initializes a new instance of the JoltExecutionException class with a specified error code, message, and
        /// optional inner exception.
        /// </summary>
        /// <param name="code">The error code that categorizes the type of execution error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused the current exception, or null if no inner exception is specified.</param>
        public JoltExecutionException(ExceptionCode code, string message, JoltException? innerException = default) 
            : base(code, message, innerException)
        {
        }
    }
}
