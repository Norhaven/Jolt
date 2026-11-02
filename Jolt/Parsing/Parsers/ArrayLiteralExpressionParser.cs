using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class ArrayLiteralExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.ArrayLiteral;

        public ArrayLiteralExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.StartOfIndexerOrArrayLiteral);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (_reader.CurrentToken.Category != ExpressionTokenCategory.StartOfIndexerOrArrayLiteral)
            {
                return false;
            }

            _reader.ConsumeCurrent();

            var elements = new List<Expression>();

            while (_reader.CurrentToken != null && _reader.CurrentToken.Category != ExpressionTokenCategory.EndOfIndexerOrArrayLiteral)
            {
                if (!_atomParser.TryParse(context, out var element))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseArrayLiteralElementAtPosition, _reader.Position);
                }

                elements.Add(element);

                if (_reader.CurrentToken.Category == ExpressionTokenCategory.ParameterSeparator)
                {
                    _reader.ConsumeCurrent();
                    continue;
                }
            }

            _reader.ExpectAndConsume(ExpressionTokenCategory.EndOfIndexerOrArrayLiteral);

            expression = ParsePipedMethodCallsFrom(context, new ArrayLiteralExpression(elements));

            return true;
        }
    }
}
