using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents an issue found during validation of a transformer.
    /// </summary>
    public sealed class ValidationIssue
    {
        /// <summary>
        /// Gets the type of the issue found.
        /// </summary>
        public ValidationIssueType Type { get; }

        /// <summary>
        /// The exception code associated with this validation issue, which can be used to identify the specific type of issue that was found.
        /// </summary>
        public ExceptionCode Code { get; }

        /// <summary>
        /// The message associated with this validation issue, which provides additional details about the issue that was found.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the JSON path to this expression within the transformer.
        /// </summary>
        public string? TransformerExpressionPath { get; }

        /// <summary>
        /// Gets the text of the expression that encountered the issue.
        /// </summary>
        public string? ExpressionText { get; }

        /// <summary>
        /// Initializes an instance of <see cref="ValidationIssue"/> with the provided parameters.
        /// </summary>
        /// <param name="type">The validation type.</param>
        /// <param name="code">The exception code.</param>
        /// <param name="message">The message.</param>
        /// <param name="transformerExpressionPath">The JSON path to the failing expression.</param>
        /// <param name="expressionText">The text of the failed expression.</param>
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
