using Jolt.Exceptions;
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
    }
}
