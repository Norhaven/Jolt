using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Represents a standard exception that may occur during the execution of a Jolt transformation.
    /// </summary>
    public abstract class JoltException : Exception
    {
        /// <summary>
        /// Gets the exception code that describes this particular exception.
        /// </summary>
        public ExceptionCode Code { get; }

        /// <summary>
        /// Gets the location within the expression being parsed that this exception is about, when known.
        /// </summary>
        public Parsing.ExpressionSpan? Span { get; private set; }

        internal JoltException WithSpan(Parsing.ExpressionSpan? span)
        {
            Span = span;
            return this;
        }

        /// <summary>
        /// Initializes a new instance of the JoltException class with a specified error code, message, and optional
        /// inner exception.
        /// </summary>
        /// <param name="code">The error code that identifies the specific type of error that occurred.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
        public JoltException(ExceptionCode code, string message, JoltException? innerException = default)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}
