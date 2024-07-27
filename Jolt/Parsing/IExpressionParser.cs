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
        bool TryParseLiteral(IEnumerable<ExpressionToken> tokens, out LiteralExpression? literal);
        bool TryParseMethod(IEnumerable<ExpressionToken> tokens, IMethodReferenceResolver referenceResolver, out MethodCallExpression? methodCall);
        bool TryParsePath(IEnumerable<ExpressionToken> tokens, out PathExpression? path);
    }
}
