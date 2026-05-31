using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    /// <summary>
    /// Represents the context in which an expression is being evaluated.
    /// </summary>
    public sealed class EvaluationContext
    {
        /// <summary>
        /// The evaluation mode (e.g. whether this is a property name or value).
        /// </summary>
        public EvaluationMode Mode { get; }
        
        /// <summary>
        /// The expression that is being evaluated.
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        /// The transformation context in which the JSON document is being evaluated against the transformer.
        /// </summary>
        public IJsonContext JsonContext { get; }
                
        /// <summary>
        /// The evaluation token that is being processed.
        /// </summary>
        public EvaluationToken Token { get; }

        /// <summary>
        /// The scope in which the expression is being evaluated. This contains any variables that are in scope for the expression, as well as any parent scopes that may be relevant for variable resolution.
        /// </summary>
        public IEvaluationScope Scope { get; }

        /// <summary>
        /// A function that can be used to transform an evaluation token within the given scope.
        /// </summary>
        public Func<EvaluationToken, IEvaluationScope, IJsonToken> Transform { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
        /// </summary>
        /// <param name="mode">The evaluation mode (e.g. whether this is a property name or value).</param>
        /// <param name="expression">The expression that is being evaluated.</param>
        /// <param name="jsonContext">The transformation context in which the JSON document is being evaluated against the transformer.</param>
        /// <param name="token">The evaluation token that is being processed.</param>
        /// <param name="scope">The scope in which the expression is being evaluated. This contains any variables that are in scope for the expression, as well as any parent scopes that may be relevant for variable resolution.</param>
        /// <param name="transform">A function that can be used to transform an evaluation token within the given scope.</param>
        public EvaluationContext(EvaluationMode mode, Expression expression, IJsonContext jsonContext, EvaluationToken token, IEvaluationScope scope, Func<EvaluationToken, IEvaluationScope, IJsonToken> transform)
        {
            Mode = mode;
            Expression = expression;
            JsonContext = jsonContext;
            Token = token;
            Scope = scope;
            Transform = transform;
        }
    }
}
