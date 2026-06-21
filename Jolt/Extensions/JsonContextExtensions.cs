using Jolt.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class JsonContextExtensions
    {
        public static JoltException CreateParsingErrorFor<T>(this IJsonContext context, ExceptionCode exceptionCode, params object[] parameters)
        {
            return context.MessageProvider.CreateErrorFor<T>(MessageCategory.Parsing, exceptionCode, parameters);
        }

        public static JoltException CreateExecutionErrorFor<T>(this IJsonContext context, ExceptionCode exceptionCode, params object[] parameters)
        {
            return context.MessageProvider.CreateErrorFor<T>(MessageCategory.Execution, exceptionCode, parameters);
        }

        public static void WriteDebugFor<T>(this IJsonContext context, string message, params object[] parameters)
        {
            context.MessageProvider.WriteDebugFor<T>(message, parameters);
        }

        public static void WriteInfoFor<T>(this IJsonContext context, string message, params object[] parameters)
        {
            context.MessageProvider.WriteInfoFor<T>(message, parameters);
        }

        public static void WriteWarningFor<T>(this IJsonContext context, string message, params object[] parameters)
        {
            context.MessageProvider.WriteWarningFor<T>(message, parameters);
        }

        public static ExecutionTraceScope CreateExecutionTraceScope(this IJsonContext context, string? transformerName = default)
        {
            return context.MessageProvider.CreateExecutionTraceScope(transformerName);
        }
    }
}
