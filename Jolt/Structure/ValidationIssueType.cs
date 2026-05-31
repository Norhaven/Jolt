using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public enum ValidationIssueType
    {
        Unknown,
        SyntaxError,
        UnknownMethod,
        ArgumentCountMismatch,
        InvalidMethodContext, // right method, wrong place
        UndeclaredVariable,
        UnsafeMethodUsage,
        MissingTransformerReference,
        InvalidLambdaShape
    }
}
