using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Jolt.Parsing
{
    /// <summary>
    /// Represents a category of expression token.
    /// </summary>
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
        [Description("a logical AND operator")]
        LogicalAnd,
        [Description("a logical OR operator")]
        LogicalOr,
        [Description("a logical NOT operator")]
        LogicalNot,
        [Description("an open parentheses group")]
        OpenParenthesesGroup,
        [Description("a close parentheses group")]
        CloseParenthesesGroup,
        [Description("a range expression")]
        RangeExpression,
        [Description("a range expression operator")]
        RangeExpressionOperator,
        [Description("an index from the end operator")]
        IndexFromEndOperator,
        [Description("a range variable")]
        RangeVariable,
        [Description("a lambda function separator or object literal property separator")]
        LambdaSeparatorOrObjectLiteralPropertySeparator,
        [Description("a property dereference")]
        PropertyDereference,
        [Description("the 'in' keyword")]
        In,
        [Description("the 'as' keyword")]
        As,
        [Description("the 'into' keyword")]
        Into,
        [Description("the start of an indexer group or array literal")]
        StartOfIndexerOrArrayLiteral,
        [Description("the end of an indexer group or array literal")]
        EndOfIndexerOrArrayLiteral,
        [Description("the start of an object literal")]
        StartOfObjectLiteral,
        [Description("the end of an object literal")]
        EndOfObjectLiteral,
        [Description("the index-from-end character for a range")]
        RangeEndIndexer,
        [Description("the null coalescing operator")]
        NullCoalescing,
        [Description("a null-safe property dereference")]
        NullSafePropertyDereference,
        [Description("a null-safe variable dereference")]
        NullSafeRangeVariableDereference,
        [Description("a null literal")]
        NullLiteral
    }
}
