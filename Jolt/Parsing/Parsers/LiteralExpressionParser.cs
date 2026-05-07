using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class LiteralExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.Literal;

        public LiteralExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser)
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.IndexFromEndOperator,
                                ExpressionTokenCategory.Subtraction,
                                ExpressionTokenCategory.NumericLiteral,
                                ExpressionTokenCategory.BooleanLiteral,
                                ExpressionTokenCategory.StringLiteral,
                                ExpressionTokenCategory.Subtraction,
                                ExpressionTokenCategory.NullLiteral);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            var isEndIndexer = IsInCategory(ExpressionTokenCategory.IndexFromEndOperator);

            if (isEndIndexer)
            {
                _reader.ConsumeCurrent();
            }

            var isNegative = _reader.CurrentToken.Category == ExpressionTokenCategory.Subtraction;

            if (isNegative)
            {
                _reader.ConsumeCurrent();
            }

            var literal = _reader.CurrentToken.Category switch
            {
                ExpressionTokenCategory.NumericLiteral when _reader.CurrentToken.Value.Contains('.') => new LiteralExpression(typeof(double), _reader.CurrentToken.Value),
                ExpressionTokenCategory.NumericLiteral => new LiteralExpression(typeof(long), _reader.CurrentToken.Value),
                ExpressionTokenCategory.BooleanLiteral => new LiteralExpression(typeof(bool), _reader.CurrentToken.Value),
                ExpressionTokenCategory.StringLiteral => new LiteralExpression(typeof(string), _reader.CurrentToken.Value),
                ExpressionTokenCategory.NullLiteral => new LiteralExpression(typeof(object), _reader.CurrentToken.Value),
                _ => default
            };

            expression = literal;

            var isParseSuccessful = expression != null;

            if (isParseSuccessful)
            {
                if (isNegative)
                {
                    if (_reader.CurrentToken.Category == ExpressionTokenCategory.NumericLiteral)
                    {
                        expression = new LiteralExpression(literal.Type, $"-{literal.Value}");
                    }
                    else
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedNumericLiteralFollowingNegativeSign, literal.Value);
                    }
                }

                _reader.ConsumeCurrent();
            }
            else if (isNegative)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedNumericLiteralFollowingNegativeSign, _reader.CurrentToken.Value);
            }

            return isParseSuccessful;
        }
    }
}
