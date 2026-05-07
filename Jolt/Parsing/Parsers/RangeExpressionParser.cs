using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class RangeExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.RangeExpression;

        public RangeExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context) => IsInCategory(ExpressionTokenCategory.RangeExpressionOperator, ExpressionTokenCategory.RangeEndIndexer);

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            var isRangeFromStart = IsInCategory(ExpressionTokenCategory.RangeExpressionOperator);

            var startIndex = isRangeFromStart ? new RangeIndexExpression(new LiteralExpression(typeof(long), "0"), false) : ParseIndexExpression(context);
            
            if (!IsInCategory(ExpressionTokenCategory.RangeExpressionOperator))
            {   
                expression = new RangeExpression(startIndex, startIndex);

                return true;
            }

            _reader.ConsumeCurrent();

            var isRangeToEnd = IsInCategory(ExpressionTokenCategory.EndOfIndexerOrArrayLiteral, ExpressionTokenCategory.ParameterSeparator, ExpressionTokenCategory.CloseParenthesesGroup);

            var endIndex = isRangeToEnd ? new RangeIndexExpression(new LiteralExpression(typeof(long), "0"), true) : ParseIndexExpression(context);
            
            expression = new RangeExpression(startIndex, endIndex);

            return true;
        }

        private RangeIndexExpression ParseIndexExpression(IJsonContext context)
        {
            var isOffsetFromEnd = _reader.CurrentToken?.Category == ExpressionTokenCategory.RangeEndIndexer;

            if (isOffsetFromEnd)
            {
                _reader.ConsumeCurrent();
            }

            if (!_atomParser.TryParse(context, out var indexExpression))
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseRangeIndexExpressionAtPosition, _reader.Position);
            }

            return new RangeIndexExpression(indexExpression, isOffsetFromEnd);
        }
    }
}
