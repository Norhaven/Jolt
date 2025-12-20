using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Represents an exception that occurs when a specific method cannot be resolved during Jolt transformation.
    /// </summary>
    public class JoltMethodResolutionException : JoltException
    {
        /// <summary>
        /// Gets the type name where the method resolution was attempted.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the method name that was attempted to be resolved.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Initializes a new instance of the JoltMethodResolutionException class with the specified exception code,
        /// type name, method name, and error message.
        /// </summary>
        /// <param name="code">The exception code that categorizes the error condition.</param>
        /// <param name="typeName">The name of the type in which the method resolution failed.</param>
        /// <param name="methodName">The name of the method that could not be resolved.</param>
        /// <param name="message">The error message that describes the reason for the exception.</param>
        public JoltMethodResolutionException(ExceptionCode code, string typeName, string methodName, string message) 
            : base(code, message)
        {
            TypeName = typeName;
            MethodName = methodName;
        }
    }
}
