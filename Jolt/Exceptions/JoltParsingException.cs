using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Represents an exception that occurred during the parsing of the Jolt transformer prior to execution.
    /// </summary>
    public sealed class JoltParsingException : JoltException
    {
        /// <summary>
        /// Initializes a new instance of the JoltParsingException class with a specified error code, message, and
        /// optional inner exception.
        /// </summary>
        /// <param name="code">The error code that categorizes the parsing exception.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
        public JoltParsingException(ExceptionCode code, string message, JoltException? innerException = default)
            : base(code, message, innerException) { }
    }
}
