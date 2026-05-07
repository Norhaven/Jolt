using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
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

            var comparisonOperatorCount = GetComparisonOperatorCount(expression);

            if (comparisonOperatorCount > 1)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedZeroOrOneComparisonSymbolsInExpressionButFoundMoreThanOne, comparisonOperatorCount);
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
                Operator.Equals => 0,
                Operator.NotEquals => 0,
                Operator.LessThan => 0,
                Operator.GreaterThan => 0,
                Operator.LessThanOrEquals => 0,
                Operator.GreaterThanOrEquals => 0,
                Operator.Addition => 1,
                Operator.Subtraction => 1,
                Operator.Multiplication => 2,
                Operator.Division => 2,
                Operator.NullCoalescing => 3,
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
                    var adjustedMinimumPrecedence = operatorPrecedence + (lookaheadPrecedence > operatorPrecedence ? 1 : 0);

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
                _ => Operator.Unknown
            };
        }

        private int GetComparisonOperatorCount(Expression expression)
        {
            if (expression is BinaryExpression binary)
            {
                var leftCount = GetComparisonOperatorCount(binary.Left);
                var rightCount = GetComparisonOperatorCount(binary.Right);

                var isComparisonOperator = binary.Operator.IsAnyOf(Operator.NotEquals, Operator.Equals, Operator.LessThan, Operator.GreaterThan, Operator.LessThanOrEquals, Operator.GreaterThanOrEquals);
                var comparisonCount = isComparisonOperator ? 1 : 0;

                return comparisonCount + leftCount + rightCount;
            }

            return 0;
        }
    }
}
