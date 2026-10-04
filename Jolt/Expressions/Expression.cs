using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public abstract class Expression
    {
        /// <summary>
        /// Gets the location within the expression text that this expression refers to, when known. For a method call,
        /// this is the method name (e.g. "#valueOf"), and for a variable, it's the variable (e.g. "@x").
        /// </summary>
        public ExpressionSpan? Span { get; internal set; }
    }

    internal static class ExpressionSpanExtensions
    {
        /// <summary>
        /// Sets the location of an expression within the expression text, and returns it.
        /// </summary>
        public static T WithSpan<T>(this T expression, ExpressionSpan? span) where T : Expression
        {
            expression.Span = span;
            return expression;
        }
    }
}
