using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public enum ExpressionTokenCategory
    {
        Unknown = 0,
        Identifier,
        StartOfMethodCall,
        StartOfPipedMethodCall,
        StartOfMethodParameters,
        ParameterSeparator,
        EndOfMethodCallAndParameters,
        StringLiteral,
        NumericLiteral,
        BooleanLiteral,
        PathLiteral,
        GeneratedNameIdentifier,
        EqualComparison,
        LessThanComparison,
        GreaterThanComparison,
        GreaterThanOrEqualComparison,
        LessThanOrEqualComparison,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        NotEqualComparison
    }
}
