using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public sealed class ErrorHandler : IErrorHandler
    {
        private readonly JoltOptions _options;

        public bool IsEnabled => _options.ErrorMode != JoltErrorMode.Strict;

        public ErrorHandler(JoltOptions? options)
        {
            _options = options ?? JoltOptions.Default;
        }

        public void HandleFor<T>(Exception exception)
        {
            if (!IsEnabled)
            {
                return;
            }

            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogError(exception, "Caught and handled exception: {Exception}", exception);
        }
    }
}
