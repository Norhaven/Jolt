using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    /// <summary>
    /// Represents a way to evaluate and execute a Jolt expression.
    /// </summary>
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Evaluate and execute a parsed Jolt expression tree within the bounds of the provided context.
        /// </summary>
        /// <param name="context">The context that the evaluation will take place within.</param>
        /// <returns>An instance of <see cref="EvaluationResult"/> that contains the results of the evaluation.</returns>
        EvaluationResult Evaluate(EvaluationContext context);
    }
}
