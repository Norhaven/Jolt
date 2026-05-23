using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class AtomExpressionParser : IAtomExpressionParser
    {
        private readonly ExpressionReader _reader;

        public IEnumerable<ISpecializedExpressionParser> AvailableParsers { get; internal set; } = Enumerable.Empty<ISpecializedExpressionParser>();

        public AtomType AtomType => AtomType.Atom;

        public AtomExpressionParser(ExpressionReader reader)            
        {
            _reader = reader;
        }

        public T GetExpressionParserOf<T>() where T : ISpecializedExpressionParser
        {
            var parser = AvailableParsers.OfType<T>().FirstOrDefault();

            if (parser == null)
            {
                throw new InvalidOperationException($"No parser of type {typeof(T).FullName} is available.");
            }

            return parser;
        }

        public bool CanParse(IJsonContext context) => AvailableParsers.Any(x => x.CanParse(context));

        public Expression? Parse(IJsonContext context)
        {
            if (!TryParse(context, out var expression))
            {
                return default;
            }

            return expression;
        }

        public bool TryParse(IJsonContext context, out Expression? expression)
        {
            var leftExpression = ReadNextAtom(context);

            expression = ParsePrecedenceExpression(leftExpression, 0, context);

            var comparisonOperatorCount = VerifyOperatorCount(expression, context, ExceptionCode.ExpectedZeroOrOneComparisonSymbolsInExpressionButFoundMoreThanOne, Operator.LessThan, Operator.GreaterThan, Operator.LessThanOrEquals, Operator.GreaterThanOrEquals);

            if (comparisonOperatorCount > 1)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedZeroOrOneComparisonSymbolsInExpressionButFoundMoreThanOne, comparisonOperatorCount);
            }

            var equalityOperatorCount = VerifyOperatorCount(expression, context, ExceptionCode.ExpectedZeroOrOneEqualitySymbolsInExpressionButFoundMoreThanOne, Operator.Equals, Operator.NotEquals);

            if (equalityOperatorCount > 1)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedZeroOrOneEqualitySymbolsInExpressionButFoundMoreThanOne, equalityOperatorCount);
            }

            return true;
        }

        private Expression? ReadNextAtom(IJsonContext context)
        {
            return AvailableParsers.FirstOrDefault(p => p.CanParse(context))?.Parse(context);
        }

        private int GetOperatorPrecedence(Operator @operator)
        {
            return @operator switch
            {
                Operator.Multiplication => 6,
                Operator.Division => 6,
                Operator.Addition => 5,
                Operator.Subtraction => 5,
                Operator.LessThan => 4,
                Operator.GreaterThan => 4,
                Operator.LessThanOrEquals => 4,
                Operator.GreaterThanOrEquals => 4,
                Operator.Equals => 3,
                Operator.NotEquals => 3,
                Operator.LogicalAnd => 2,
                Operator.LogicalOr => 1,
                Operator.NullCoalescing => 0,
                _ => -1
            };
        }

        private Expression ParsePrecedenceExpression(Expression leftExpression, int minimumPrecedence, IJsonContext context)
        {
            var lookahead = ToOperator(_reader.CurrentToken);
            var lookaheadPrecedence = GetOperatorPrecedence(lookahead);

            while (lookaheadPrecedence >= minimumPrecedence)
            {
                var @operator = lookahead;
                var operatorPrecedence = GetOperatorPrecedence(@operator);

                _reader.ConsumeCurrent();

                var rightExpression = ReadNextAtom(context);

                lookahead = ToOperator(_reader.CurrentToken);
                lookaheadPrecedence = GetOperatorPrecedence(lookahead);

                while (lookaheadPrecedence > operatorPrecedence)
                {
                    var isRightAssociative = @operator == Operator.NullCoalescing;
                    var adjustedMinimumPrecedence = isRightAssociative ? operatorPrecedence : operatorPrecedence + 1;

                    rightExpression = ParsePrecedenceExpression(rightExpression, adjustedMinimumPrecedence, context);

                    lookahead = ToOperator(_reader.CurrentToken);
                    lookaheadPrecedence = GetOperatorPrecedence(lookahead);
                }

                leftExpression = new BinaryExpression(leftExpression, @operator, rightExpression);
            }

            return leftExpression;
        }

        private Operator ToOperator(ExpressionToken token)
        {
            return token?.Category switch
            {
                ExpressionTokenCategory.EqualComparison => Operator.Equals,
                ExpressionTokenCategory.GreaterThanComparison => Operator.GreaterThan,
                ExpressionTokenCategory.LessThanComparison => Operator.LessThan,
                ExpressionTokenCategory.GreaterThanOrEqualComparison => Operator.GreaterThanOrEquals,
                ExpressionTokenCategory.LessThanOrEqualComparison => Operator.LessThanOrEquals,
                ExpressionTokenCategory.Addition => Operator.Addition,
                ExpressionTokenCategory.Subtraction => Operator.Subtraction,
                ExpressionTokenCategory.Multiplication => Operator.Multiplication,
                ExpressionTokenCategory.Division => Operator.Division,
                ExpressionTokenCategory.NotEqualComparison => Operator.NotEquals,
                ExpressionTokenCategory.NullCoalescing => Operator.NullCoalescing,
                ExpressionTokenCategory.LogicalAnd => Operator.LogicalAnd,
                ExpressionTokenCategory.LogicalOr => Operator.LogicalOr,
                _ => Operator.Unknown
            };
        }

        private int VerifyOperatorCount(Expression expression, IJsonContext context, ExceptionCode raisedErrorCode, params Operator[] operators)
        {
            if (expression is BinaryExpression binary)
            {
                var leftCount = VerifyOperatorCount(binary.Left, context, raisedErrorCode, operators);
                var rightCount = VerifyOperatorCount(binary.Right, context, raisedErrorCode, operators);

                if (binary.Operator.IsAnyOf(Operator.LogicalAnd, Operator.LogicalOr, Operator.NullCoalescing))
                {
                    if (leftCount > 1)
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(raisedErrorCode, leftCount);
                    }

                    if (rightCount > 1)
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(raisedErrorCode, rightCount);
                    }

                    // Subtrees are fully validated above; report 0 upward so the
                    // parent doesn't accumulate counts that have already been checked.

                    return 0;
                }

                var isTargetOperator = binary.Operator.IsAnyOf(operators);
                
                return (isTargetOperator ? 1 : 0) + leftCount + rightCount;
            }

            return 0;
        }
    }
}
