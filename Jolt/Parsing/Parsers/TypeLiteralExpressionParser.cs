using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class TypeLiteralExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.TypeLiteral;

        public TypeLiteralExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.TypeLiteral);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (!IsInCategory(ExpressionTokenCategory.TypeLiteral))
            {
                return false;
            }

            expression = new TypeLiteralExpression(_reader.ConsumeCurrent().Value);

            return true;
        }
    }
}
