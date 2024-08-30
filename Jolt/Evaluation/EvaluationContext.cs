using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class EvaluationContext
    {
        public EvaluationMode Mode { get; }
        public Expression Expression { get; }
        public IJsonContext JsonContext { get; }
        public EvaluationToken Token { get; }
        public IEvaluationScope Scope { get; }
        public Func<EvaluationToken, IEvaluationScope, IJsonToken> Transform { get; }

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
