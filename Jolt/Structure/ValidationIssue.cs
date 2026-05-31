using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public sealed class ValidationIssue
    {
        public ValidationIssueType Type { get; }
        public ExceptionCode Code { get; }
        public string Message { get; }
        public string? TransformerExpressionPath { get; }
        public string? ExpressionText { get; }

        public ValidationIssue(ValidationIssueType type, ExceptionCode code, string message, string? transformerExpressionPath, string? expressionText)
        {
            Type = type;
            Code = code;
            Message = message;
            TransformerExpressionPath = transformerExpressionPath;
            ExpressionText = expressionText;
        }
    }
}
