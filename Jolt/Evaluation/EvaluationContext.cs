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
        public IJsonContext Context { get; }
        public EvaluationToken Token { get; }
        public Stack<IJsonToken> ClosureSources { get; }
        public Func<EvaluationToken, Stack<IJsonToken>, IJsonToken> Transform { get; }

        public EvaluationContext(EvaluationMode mode, Expression expression, IJsonContext context, EvaluationToken token, Stack<IJsonToken> closureSources, Func<EvaluationToken, Stack<IJsonToken>, IJsonToken> transform)
        {
            Mode = mode;
            Expression = expression;
            Context = context;
            Token = token;
            ClosureSources = closureSources;
            Transform = transform;
        }
    }
}
