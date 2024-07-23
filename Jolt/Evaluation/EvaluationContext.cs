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
        public Stack<IJsonToken> ClosureSources { get; }
        public Func<EvaluationToken, Stack<IJsonToken>, IJsonToken> Transform { get; }

        public EvaluationContext(EvaluationMode mode, Expression expression, IJsonContext jsonContext, EvaluationToken token, Stack<IJsonToken> closureSources, Func<EvaluationToken, Stack<IJsonToken>, IJsonToken> transform)
        {
            Mode = mode;
            Expression = expression;
            JsonContext = jsonContext;
            Token = token;
            ClosureSources = closureSources;
            Transform = transform;
        }
    }
}
