using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Jolt.Parsing
{
    public enum ExpressionTokenCategory
    {
        Unknown = 0,
        [Description("an identifier")]
        Identifier,
        [Description("the start of a method call")]
        StartOfMethodCall,
        [Description("the start of a piped method call")]
        StartOfPipedMethodCall,
        [Description("the start of method parameters")]
        StartOfMethodParameters,
        [Description("a parameter separator")]
        ParameterSeparator,
        [Description("a string literal")]
        StringLiteral,
        [Description("a numeric literal")]
        NumericLiteral,
        [Description("a boolean literal")]
        BooleanLiteral,
        [Description("a path literal")]
        PathLiteral,
        [Description("a generated name identifier")]
        GeneratedNameIdentifier,
        [Description("an equality comparison")]
        EqualComparison,
        [Description("a less than comparison")]
        LessThanComparison,
        [Description("a greater than comparison")]
        GreaterThanComparison,
        [Description("a greater than or equal comparison")]
        GreaterThanOrEqualComparison,
        [Description("a less than or equal comparison")]
        LessThanOrEqualComparison,
        [Description("an addition operator")]
        Addition,
        [Description("a subtraction operator")]
        Subtraction,
        [Description("a multiplication operator")]
        Multiplication,
        [Description("a division operator")]
        Division,
        [Description("an inequality comparison")]
        NotEqualComparison,
        [Description("an open parentheses group")]
        OpenParenthesesGroup,
        [Description("a close parentheses group")]
        CloseParenthesesGroup
    }
}
