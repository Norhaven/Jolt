using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
            expression = default;

            var isParsed = false;
            
            // Parse the unary expressions first and then, if successful,
            // see if it's part of a larger binary expression.

            if (TryParseMethod(reader, referenceResolver, out var method))
            {
                expression = method;
                isParsed = true;
            }
            else if (TryParsePath(reader, out var path))
            {
                // Paths are only valid within a unary context and can't be
                // used as a part of a binary expression by themselves.

                expression = path;
                return true;
            }
            else if (TryParseLiteral(reader, out var literal))
            {
                expression = literal;
                isParsed = true;
            }
            
            if (!isParsed)
            {
                return false;
            }

            if (TryParseComparisonOrMath(reader, expression, referenceResolver, out var comparison))
            {
                expression = comparison;
                return true;
            }

            return isParsed;
        }

        private bool TryParseComparisonOrMath(ExpressionReader reader, Expression leftSide, IReferenceResolver referenceResolver, out Expression rightSide)
        {
            int GetOperatorPrecedence(Operator @operator)
            {
                return @operator switch
                {
                    Operator.Equal => 0,
                    Operator.NotEqual => 0,
                    Operator.LessThan => 0,
                    Operator.GreaterThan => 0,
                    Operator.LessThanOrEquals => 0,
                    Operator.GreaterThanOrEquals => 0,
                    Operator.Addition => 1,
                    Operator.Subtraction => 1,
                    Operator.Multiplication => 2,
                    Operator.Division => 2,
                    _ => -1
                };
            }

            Expression ParsePrecedenceExpression(ExpressionReader reader, Expression leftExpression, int minimumPrecedence)
            {
                var lookahead = ToOperator(reader.CurrentToken);
                var lookaheadPrecedence = GetOperatorPrecedence(lookahead);

                while(lookaheadPrecedence >= minimumPrecedence)
                {
                    var @operator = lookahead;
                    var operatorPrecedence = GetOperatorPrecedence(@operator);

                    reader.ConsumeCurrent();

                    Expression rightExpression = default;

                    if (TryParseMethod(reader, referenceResolver, out var methodCall))
                    {
                        rightExpression = methodCall;
                    }
                    else if (TryParseLiteral(reader, out var literal))
                    {
                        rightExpression = literal;
                    }
                    else
                    {
                        throw new JoltParsingException($"Unable to parse the right hand side of an expression with operator '{@operator}'");
                    }

                    lookahead = ToOperator(reader.CurrentToken);
                    lookaheadPrecedence = GetOperatorPrecedence(lookahead);

                    while(lookaheadPrecedence > operatorPrecedence)
                    {
                        var adjustedMinimumPrecedence = operatorPrecedence + (lookaheadPrecedence > operatorPrecedence ? 1 : 0);

                        rightExpression = ParsePrecedenceExpression(reader, rightExpression, adjustedMinimumPrecedence);

                        lookahead = ToOperator(reader.CurrentToken);
                        lookaheadPrecedence = GetOperatorPrecedence(lookahead);
                    }

                    leftExpression = new BinaryExpression(leftExpression, @operator, rightExpression);
                }

                return leftExpression;
            }

            Operator ToOperator(ExpressionToken token)
            {
                return token?.Category switch
                {
                    ExpressionTokenCategory.EqualComparison => Operator.Equal,
                    ExpressionTokenCategory.GreaterThanComparison => Operator.GreaterThan,
                    ExpressionTokenCategory.LessThanComparison => Operator.LessThan,
                    ExpressionTokenCategory.GreaterThanOrEqualComparison => Operator.GreaterThanOrEquals,
                    ExpressionTokenCategory.LessThanOrEqualComparison => Operator.LessThanOrEquals,
                    ExpressionTokenCategory.Addition => Operator.Addition,
                    ExpressionTokenCategory.Subtraction => Operator.Subtraction,
                    ExpressionTokenCategory.Multiplication => Operator.Multiplication,
                    ExpressionTokenCategory.Division => Operator.Division,
                    ExpressionTokenCategory.NotEqualComparison => Operator.NotEqual,
                    _ => Operator.Unknown
                };
            }

            rightSide = default;

            var @operator = ToOperator(reader.CurrentToken);

            if (@operator == Operator.Unknown)
            {
                return false;
            }

            rightSide = ParsePrecedenceExpression(reader, leftSide, 0);

            return true;
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

            var isParseSuccessful = literal != null;

            if (isParseSuccessful)
            {
                reader.ConsumeCurrent();
            }

            return isParseSuccessful;
        }

        private bool TryParseMethod(ExpressionReader reader, IReferenceResolver referenceResolver, out MethodCallExpression? methodCall)
        {
            methodCall = default;

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodCall || 
                                                    x.Category == ExpressionTokenCategory.StartOfPipedMethodCall))
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

            if (reader.CurrentToken.Category != ExpressionTokenCategory.EndOfMethodCallAndParameters)
            {
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
                while (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.ParameterSeparator));
            }

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.EndOfMethodCallAndParameters))
            {
                return false;
            }            

            var methodSignature = referenceResolver.GetMethod(potentiallyQualifiedMethodName.Value);

            if (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.GeneratedNameIdentifier, out var generatedName))
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), generatedName.Value);
            }
            else if (reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfPipedMethodCall)
            {
                if (!TryParseMethod(reader, referenceResolver, out var pipedMethodCall))
                {
                    throw new JoltParsingException("Expected piped method call but could not complete parsing it");
                }

                // We're piping the initial method call results into the first argument of the target method,
                // so we need to switch the evaluation order around a little bit.

                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), default);

                var updatedParameters = new[] { methodCall }.Concat(pipedMethodCall.ParameterValues);

                methodCall = pipedMethodCall.WithParameters(updatedParameters);
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
