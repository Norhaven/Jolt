using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents the type of issue encountered.
    /// </summary>
    public enum ValidationIssueType
    {
        Unknown,
        SyntaxError,
        UnknownMethod,
        ArgumentCountMismatch,
        InvalidMethodContext, // For example, using a property value-only method in a property name.
        UndeclaredVariable,
        UnsafeMethodUsage,
        MissingTransformerReference,
        InvalidLambdaShape
    }
}
