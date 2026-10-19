using Jolt.Exceptions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class EvaluationError
    {
        private readonly Exception _exception;

        public string ExceptionType => _exception.GetType().FullName ?? "Exception";
        public string Message => _exception.Message;
        public string StackTrace => _exception.StackTrace ?? string.Empty;

        /// <summary>
        /// Gets the documented Jolt error code (e.g. "JLT563") when the error was raised by Jolt, null otherwise.
        /// </summary>
        public string? Code => _exception is JoltException jolt ? jolt.Code.GetDescription() : null;

        public EvaluationError? InnerError => _exception.InnerException != null ? new EvaluationError(_exception.InnerException) : null;

        public EvaluationError(Exception exception)
        {
            _exception = Unwrap(exception);
        }

        private static Exception Unwrap(Exception exception)
        {
            // Method calls are made through reflection and then wrapped to name the method that failed, neither of
            // which helps an error handler decide what went wrong, so surface the error that actually caused it.

            while (exception.InnerException != null)
            {
                var isWrapper = exception is TargetInvocationException ||
                    (exception is JoltException jolt && jolt.Code == ExceptionCode.ExternalMethodInvocationCausedAnException);

                if (!isWrapper)
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return exception;
        }
    }
}
