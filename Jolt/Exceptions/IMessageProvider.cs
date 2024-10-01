using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Represents a way to provide messages to the caller, such as errors or warnings.
    /// </summary>
    public interface IMessageProvider
    {
        /// <summary>
        /// Creates a method resolution error for the provided type and method name.
        /// </summary>
        /// <typeparam name="T">The type containing the invalid resolution attempt.</typeparam>
        /// <param name="exceptionCode">The exception code for this error.</param>
        /// <param name="typeName">The type name associated with the error.</param>
        /// <param name="methodName">The method name associated with the error.</param>
        /// <param name="parameters">Parameters to apply to the message template.</param>
        /// <returns>An instance of <see cref="JoltException"/> with the error information.</returns>
        JoltException CreateResolutionErrorFor<T>(ExceptionCode exceptionCode, string typeName, string methodName, params object[] parameters);

        /// <summary>
        /// Creates a categorized error for the provided exception code.
        /// </summary>
        /// <typeparam name="T">The type containing the invalid attempt.</typeparam>
        /// <param name="category">The category for this particular message.</param>
        /// <param name="exceptionCode">The exception code for this error.</param>
        /// <param name="parameters">Parameters to apply to the message template.</param>
        /// <returns>An instance of <see cref="JoltException"/> with the error information.</returns>
        JoltException CreateErrorFor<T>(MessageCategory category, ExceptionCode exceptionCode, params object[] parameters);

        /// <summary>
        /// Creates a categorized error for the provided exception code with an inner exception.
        /// </summary>
        /// <typeparam name="T">The type containing the invalid attempt.</typeparam>
        /// <param name="category">The category for this particular message.</param>
        /// <param name="exceptionCode">The exception code for this error.</param>
        /// <param name="parameters">Parameters to apply to the message template.</param>
        /// <returns>An instance of <see cref="JoltException"/> with the error information.</returns>
        JoltException CreateErrorFor<T>(MessageCategory category, ExceptionCode exceptionCode, JoltException innerException, params object[] parameters);

        /// <summary>
        /// Writes an information level message.
        /// </summary>
        /// <typeparam name="T">The type containing the code related to the message.</typeparam>
        /// <param name="message">The message template to use.</param>
        /// <param name="parameters">Parameters to apply to the message template.</param>
        void WriteInfoFor<T>(string message, params object[] parameters);

        /// <summary>
        /// Writes a debug level message.
        /// </summary>
        /// <typeparam name="T">The type containing the code related to the message.</typeparam>
        /// <param name="message">The message template to use.</param>
        /// <param name="parameters">Parameters to apply to the message template.</param>
        void WriteDebugFor<T>(string message, params object[] parameters);

        /// <summary>
        /// Writes a warning level message.
        /// </summary>
        /// <typeparam name="T">The type containing the code related to the message.</typeparam>
        /// <param name="message">The message template to use.</param>
        /// <param name="parameters">Parameters to apply to the message template.</param>
        void WriteWarningFor<T>(string message, params object[] parameters);
    }
}
