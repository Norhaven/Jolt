using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    /// <summary>
    /// Represents a handler for errors that occur during the transformation process.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Gets whether this handler is enabled and therefore should handle any errors.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Handle the provided exception for the specified type.
        /// </summary>
        /// <typeparam name="T">The type that is handling this specific error.</typeparam>
        /// <param name="exception">The exception that should be handled.</param>
        void HandleFor<T>(Exception exception);
    }
}
