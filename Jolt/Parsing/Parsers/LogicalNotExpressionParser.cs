using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class LogicalNotExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.LogicalNot;

        public LogicalNotExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser)
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.LogicalNot);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (!IsInCategory(ExpressionTokenCategory.LogicalNot))
            {
                return false;
            }

            _reader.ConsumeCurrent();

            var expressionToNegate = _atomParser.Parse(context);

            expression = new LogicalNotExpression(expressionToNegate);

            return true;
        }
    }
}
