using Jolt.Evaluation;
using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Parsing
{
    public class ExpressionParser : IExpressionParser
    {
        private sealed class ExpressionReader : TokenStream<ExpressionToken>
        {
            public ExpressionReader(IEnumerable<ExpressionToken> tokens) 
                : base(tokens)
            {
            }
        }

        public bool TryParseExpression(IEnumerable<ExpressionToken> tokens, IReferenceResolver referenceResolver, out Expression? expression) => TryParseExpression(new ExpressionReader(tokens), referenceResolver, out expression);

        public bool TryParseLiteral(IEnumerable<ExpressionToken> tokens, out LiteralExpression? literal) => TryParseLiteral(new ExpressionReader(tokens), out literal);
        
        public bool TryParseMethod(IEnumerable<ExpressionToken> tokens, IReferenceResolver referenceResolver, out MethodCallExpression? methodCall) => TryParseMethod(new ExpressionReader(tokens), referenceResolver, out methodCall);

        public bool TryParsePath(IEnumerable<ExpressionToken> tokens, out PathExpression? path) => TryParsePath(new ExpressionReader(tokens), out path);

        private bool TryParseExpression(ExpressionReader reader, IReferenceResolver referenceResolver, out Expression? expression)
        {
            if (TryParseMethod(reader, referenceResolver, out var method))
            {
                expression = method;
                return true;
            }

            if (TryParsePath(reader, out var path))
            {
                expression = path;
                return true;
            }

            if (TryParseLiteral(reader, out var literal))
            {
                expression = literal;
                return true;
            }

            expression = default;

            return false;
        }

        private bool TryParseLiteral(ExpressionReader reader, out LiteralExpression? literal)
        {
            literal = reader.CurrentToken.Category switch
            { 
                ExpressionTokenCategory.NumericLiteral => new LiteralExpression(typeof(int), reader.CurrentToken.Value),
                ExpressionTokenCategory.BooleanLiteral => new LiteralExpression(typeof(bool), reader.CurrentToken.Value),
                ExpressionTokenCategory.StringLiteral => new LiteralExpression(typeof(string), reader.CurrentToken.Value),
                _ => default
            };

            return literal != null;
        }

        private bool TryParseMethod(ExpressionReader reader, IReferenceResolver referenceResolver, out MethodCallExpression? methodCall)
        {
            methodCall = default;

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodCall))
            {
                return false;
            }

            if (!reader.TryConsumeNext(out var potentiallyQualifiedMethodName))
            {
                return false;
            }

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodParameters))
            {
                return false;
            }

            var actualParameters = new List<Expression>();

            do
            {
                if (!TryParseExpression(reader, referenceResolver, out var actualValue))
                {
                    return false;
                }

                actualParameters.Add(actualValue);

                if (reader.CurrentToken.Category == ExpressionTokenCategory.EndOfMethodCallAndParameters)
                {
                    break;
                }
            }
            while (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.ParameterSeparator));

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.EndOfMethodCallAndParameters))
            {
                return false;
            }

            var methodSignature = referenceResolver.GetMethod(potentiallyQualifiedMethodName.Value);

            if (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.GeneratedNameIdentifier, out var generatedName))
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), generatedName.Value);
            }
            else
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray());
            }

            return true;
        }

        private bool TryParsePath(ExpressionReader reader, out PathExpression? path)
        {
            path = reader.CurrentToken.Category switch
            {
                ExpressionTokenCategory.PathLiteral => new PathExpression(reader.CurrentToken.Value),
                _ => default
            };

            var isPath = path != null;

            if (isPath)
            {
                reader.ConsumeCurrent();
            }

            return isPath;
        }
    }
}
