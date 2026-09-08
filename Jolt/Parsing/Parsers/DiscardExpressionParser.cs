using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal class DiscardExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.Discard;

        public DiscardExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.Discard);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (_reader.CurrentToken.Category != ExpressionTokenCategory.Discard)
            {
                return false;
            }

            _reader.ConsumeCurrent();

            expression = new DiscardExpression();

            return true;
        }
    }
}
