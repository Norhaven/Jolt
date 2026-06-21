using Jolt.Structure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Jolt.Exceptions
{
    public sealed class MessageProvider : IMessageProvider
    {
        private readonly JoltOptions _options;
        private readonly List<ExecutionTraceEntry> _executionTraces = new List<ExecutionTraceEntry>();
        private readonly Stack<ExecutionTraceScope> _executionTraceScopes = new Stack<ExecutionTraceScope>();

        public ExecutionTraceScope? CurrentScope => _executionTraceScopes.Count > 0 ? _executionTraceScopes.Peek() : default;
        public ExecutionTraceEntry[] ExecutionTraces => _executionTraces.OrderBy(x => x.ExecutionStartedAtTicks).ToArray();

        public MessageProvider(JoltOptions options)
        {
            _options = options;
        }

        public JoltException CreateErrorFor<T>(MessageCategory category, ExceptionCode exceptionCode, params object[] parameters)
        {
            return CreateErrorFor<T>(category, exceptionCode, default, parameters);
        }

        public JoltException CreateErrorFor<T>(MessageCategory category, ExceptionCode exceptionCode, JoltException? innerException, params object[] parameters)
        {
            var logger = _options.LoggerFactory?.CreateLogger<T>();

            var exception = category switch
            {
                MessageCategory.Parsing => Error.CreateParsingErrorFrom(exceptionCode, innerException, parameters),
                MessageCategory.Execution => Error.CreateExecutionErrorFrom(exceptionCode, innerException, parameters),
                MessageCategory.Resolution => throw new NotSupportedException($"Due to missing relevant type and method information, please use CreateResolutionErrorFor<T> to create resolution errors instead of CreateErrorFor<T>"),
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
            if (!_options.IsLoggingEnabled)
            {
                return;
            }

            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogDebug(message, parameters);
        }

        public void WriteInfoFor<T>(string message, params object[] parameters)
        {
            if (!_options.IsLoggingEnabled)
            {
                return;
            }

            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogInformation(message, parameters);
        }

        public void WriteWarningFor<T>(string message, params object[] parameters)
        {
            if (!_options.IsLoggingEnabled)
            {
                return;
            }

            var logger = _options.LoggerFactory?.CreateLogger<T>();

            logger?.LogWarning(message, parameters);
        }

        public void WriteExecutionTraceFor(ExecutionTraceEntry traceEntry)
        {
            if (!_options.IsExecutionTracingEnabled)
            {
                return;
            }

            _executionTraces.Add(traceEntry);
        }

        public ExecutionTraceScope CreateExecutionTraceScope(string? transformerName = default)
        {
            var scope = CurrentScope switch
            {
                null => transformerName is null ? ExecutionTraceScope.CreateRootScopeWith(this) : new ExecutionTraceScope(this, transformerName),
                var x => x.OpenTransformerScopeFor(transformerName)
            };

            _executionTraceScopes.Push(scope);
            
            return scope;
        }

        public ExecutionTraceScope CreateRootExecutionTraceScope()
        {
            var scope = ExecutionTraceScope.CreateRootScopeWith(this);
            
            _executionTraceScopes.Push(scope);
            
            return scope;
        }

        public void RemoveCurrentScope()
        {
            if (_executionTraceScopes.Count > 0)
            {
                _executionTraceScopes.Pop();
            }
        }
    }
}
