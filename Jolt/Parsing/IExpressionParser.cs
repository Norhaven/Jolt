using Jolt.Evaluation;
using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public interface IExpressionParser
    {
        bool TryParseExpression(IEnumerable<ExpressionToken> tokens, IMethodReferenceResolver referenceResolver, out Expression? expression);
    }
}
