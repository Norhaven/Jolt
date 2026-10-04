using Jolt.Exceptions;
using Jolt.Parsing;
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
        /// Gets whether the issue is within the expression in the property name at <see cref="TransformerExpressionPath"/>,
        /// rather than the expression in its value. A property's name and value share the same path.
        /// </summary>
        public bool IsInPropertyName { get; }

        /// <summary>
        /// Gets the location of the issue within the expression (the property name or value at
        /// <see cref="TransformerExpressionPath"/>), when known. For a syntax error, this is where parsing failed, which
        /// may be an empty span at the end of the expression when it ended early.
        /// </summary>
        public ExpressionSpan? Span { get; }

        /// <summary>
        /// Initializes an instance of <see cref="ValidationIssue"/> with the provided parameters.
        /// </summary>
        /// <param name="type">The validation type.</param>
        /// <param name="code">The exception code.</param>
        /// <param name="message">The message.</param>
        /// <param name="transformerExpressionPath">The JSON path to the failing expression.</param>
        /// <param name="expressionText">The text of the failed expression.</param>
        public ValidationIssue(ValidationIssueType type, ExceptionCode code, string message, string? transformerExpressionPath, string? expressionText)
            : this(type, code, message, transformerExpressionPath, expressionText, false)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ValidationIssue"/> with the provided parameters.
        /// </summary>
        /// <param name="type">The validation type.</param>
        /// <param name="code">The exception code.</param>
        /// <param name="message">The message.</param>
        /// <param name="transformerExpressionPath">The JSON path to the failing expression.</param>
        /// <param name="expressionText">The text of the failed expression.</param>
        /// <param name="isInPropertyName">Whether the failing expression is the property name at the path, rather than its value.</param>
        public ValidationIssue(ValidationIssueType type, ExceptionCode code, string message, string? transformerExpressionPath, string? expressionText, bool isInPropertyName)
            : this(type, code, message, transformerExpressionPath, expressionText, isInPropertyName, null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ValidationIssue"/> with the provided parameters.
        /// </summary>
        /// <param name="type">The validation type.</param>
        /// <param name="code">The exception code.</param>
        /// <param name="message">The message.</param>
        /// <param name="transformerExpressionPath">The JSON path to the failing expression.</param>
        /// <param name="expressionText">The text of the failed expression.</param>
        /// <param name="isInPropertyName">Whether the failing expression is the property name at the path, rather than its value.</param>
        /// <param name="span">The location of the issue within the failing expression, if known.</param>
        public ValidationIssue(ValidationIssueType type, ExceptionCode code, string message, string? transformerExpressionPath, string? expressionText, bool isInPropertyName, ExpressionSpan? span)
        {
            Type = type;
            Code = code;
            Message = message;
            TransformerExpressionPath = transformerExpressionPath;
            ExpressionText = expressionText;
            IsInPropertyName = isInPropertyName;
            Span = span;
        }
    }
}
