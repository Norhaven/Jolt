using Jolt.Evaluation;
using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    /// <summary>
    /// Represents a way of parsing expression tokens into an expression tree.
    /// </summary>
    public interface IExpressionParser
    {
        /// <summary>
        /// Tries to parse the series of tokens into a valid expression tree.
        /// </summary>
        /// <param name="tokens">The tokens to parse.</param>
        /// <param name="referenceResolver">The way to resolve methods and ensure that they are known at parse time.</param>
        /// <param name="expression">The parsed expression tree.</param>
        /// <returns>True if the parse attempt succeeded, false otherwise.</returns>
        bool TryParseExpression(IEnumerable<ExpressionToken> tokens, IMethodReferenceResolver referenceResolver, out Expression? expression);
    }
}
