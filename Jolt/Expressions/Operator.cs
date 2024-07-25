using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public enum Operator
    {
        Unknown = 0,
        Equal,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEquals,
        GreaterThanOrEquals,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        OpenGroup,
        CloseGroup
    }
}
