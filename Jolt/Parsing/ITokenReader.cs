using Jolt.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    /// <summary>
    /// Represents a way of reading a Jolt expression into a series of tokens.
    /// </summary>
    public interface ITokenReader
    {
        /// <summary>
        /// Determines whether the given Jolt expression starts with a method call or open parentheses.
        /// This will inform decisions about the viability of a particular expression.
        /// </summary>
        /// <param name="expression">The Jolt expression to inspect.</param>
        /// <returns>True if the expression starts with a method call or open parentheses, false otherwise.</returns>
        bool StartsWithMethodCallOrOpenParenthesesOrRangeVariable(string expression);

        /// <summary>
        /// Reads an entire Jolt expression into a series of tokens.
        /// </summary>
        /// <param name="expression">The Jolt expression to read.</param>
        /// <param name="mode">The evaluation mode which will determine how a particular token is treated.</param>
        /// <returns>The Jolt expression as a series of expression tokens or an empty sequence if none were present.</returns>
        IEnumerable<ExpressionToken> ReadToEnd(string expression, EvaluationMode mode);
    }
}
