using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public sealed class MessageProvider : IMessageProvider
    {
        private readonly JoltOptions _options;

        public MessageProvider(JoltOptions options)
        {
            _options = options;
        }

        public JoltException CreateErrorFor<T>(MessageCategory category, ExceptionCode exceptionCode, params object[] parameters)
        {
            var logger = _options.LoggerFactory?.CreateLogger<T>();

            var exception = category switch
            { 
                MessageCategory.Parsing => Error.CreateParsingErrorFrom(exceptionCode, parameters),
                MessageCategory.Execution => Error.CreateExecutionErrorFrom(exceptionCode, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(category), $"Unable to create error for unsupported message category '{category}'"),
            };

            logger?.LogError(exception, "Created error for category {Category}: {Exception}", category, exception);

            return exception;
        }

        public JoltException CreateResolutionErrorFor<T>(ExceptionCode exceptionCode, string typeName, string methodName, params object[] parameters)
        {
            var logger = _options.LoggerFactory?.CreateLogger<T>();

            var exception = Error.CreateResolutionErrorFrom(exceptionCode, typeName, methodName, parameters);

            logger?.LogError(exception, "Created resolution error: {Exception}", exception);

            return exception;
        }

        public void WriteDebugFor<T>(string message, params object[] parameters)
        {
            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogDebug(message, parameters);
        }

        public void WriteInfoFor<T>(string message, params object[] parameters)
        {
            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogInformation(message, parameters);
        }

        public void WriteWarningFor<T>(string message, params object[] parameters)
        {
            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogWarning(message, parameters);
        }
    }
}
