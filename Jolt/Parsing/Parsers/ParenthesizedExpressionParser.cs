using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class ParenthesizedExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.ParenthesizedExpression;

        public ParenthesizedExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context) => IsInCategory(ExpressionTokenCategory.OpenParenthesesGroup);

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.OpenParenthesesGroup))
            {
                return false;
            }

            if (!_atomParser.TryParse(context, out var parenthesizedExpression))
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseParenthesizedExpressionAtPosition, _reader.Position);
            }

            if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.CloseParenthesesGroup))
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCloseParenthesizedExpressionAtPosition, _reader.Position);
            }

            expression = parenthesizedExpression;

            return true;
        }
    }
}
