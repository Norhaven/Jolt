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
        GeneratedNameIdentifier = 10
    }
}
