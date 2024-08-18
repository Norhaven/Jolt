using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt
{
    /// <summary>
    /// Represents options for controlling and/or enhancing the behavior of the Jolt transformer.
    /// </summary>
    public sealed class JoltOptions
    {
        /// <summary>
        /// Gets the default options for Jolt. It defaults to strict error handling but additional 
        /// options may be applied with the provided instance builder methods to change that and/or set other options. 
        /// </summary>
        public static JoltOptions Default => new JoltOptions { ErrorMode = JoltErrorMode.Strict };

        /// <summary>
        /// Gets the error mode that Jolt will operate under, either strict or loose error handling.
        /// </summary>
        public JoltErrorMode ErrorMode { get; private set; }

        public ILoggerFactory? LoggerFactory { get; private set; }

        private JoltOptions() 
        { 
        }

        /// <summary>
        /// Specifies that Jolt should not let errors stop the execution of the entire transformer.
        /// </summary>
        /// <returns>An instance of <see cref="JoltOptions"/> with the error mode change applied.</returns>
        public JoltOptions WithLooseErrorHandling()
        {
            ErrorMode = JoltErrorMode.Loose;

            return this;
        }

        /// <summary>
        /// Specifies that Jolt should use the provided logger factory to include log messages during its execution.
        /// </summary>
        /// <param name="loggerFactory">The logger factory that should be used for logging.</param>
        /// <returns>An instance of <see cref="JoltOptions"/> with the error mode change applied.</returns>
        public JoltOptions WithLogging(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;

            return this;
        }
    }
}
