using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public enum ExpressionTokenCategory
    {
        Unknown = 0,
        Identifier = 1,
        StartOfMethodCall = 2,
        StartOfMethodParameters = 3,
        ParameterSeparator = 4,
        EndOfMethodCallAndParameters = 5,
        StringLiteral = 6,
        NumericLiteral = 7,
        BooleanLiteral = 8,
        PathLiteral = 9,
        GeneratedNameIdentifier = 10,
        EqualComparison = 11,
        LessThanComparison = 12,
        GreaterThanComparison = 13,
        GreaterThanOrEqualComparison = 14,
        LessThanOrEqualComparison = 15,
        Addition = 16,
        Subtraction = 17,
        Multiplication = 18,
        Division = 19,
        NotEqualComparison = 20
    }
}
