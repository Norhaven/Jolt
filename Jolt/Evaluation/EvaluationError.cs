using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class EvaluationError
    {
        private readonly Exception _exception;

        public string ExceptionType => _exception.GetType().FullName ?? "Exception";
        public string Message => _exception.Message;
        public string StackTrace => _exception.StackTrace ?? string.Empty;
        public EvaluationError? InnerError => _exception.InnerException != null ? new EvaluationError(_exception.InnerException) : null;

        public EvaluationError(Exception exception)
        {
            _exception = exception;
        }
    }
}
