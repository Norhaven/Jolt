using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class ObjectLiteralExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.ObjectLiteral;

        public ObjectLiteralExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.StartOfObjectLiteral);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (_reader.CurrentToken.Category != ExpressionTokenCategory.StartOfObjectLiteral)
            {
                return false;
            }

            _reader.ConsumeCurrent();

            var properties = new List<ObjectLiteralPropertyExpression>();
            var literalParser = GetExpressionParserOf<LiteralExpressionParser>();

            while (_reader.CurrentToken != null && _reader.CurrentToken.Category != ExpressionTokenCategory.EndOfObjectLiteral)
            {
                if (!literalParser.TryParse(context, out var propertyName))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseObjectLiteralPropertyNameAtPosition, _reader.Position);
                }

                var propertyNameLiteral = (LiteralExpression)propertyName!;

                if (propertyNameLiteral.Type != typeof(string))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ObjectLiteralPropertyNameMustBeStringAtPosition, _reader.Position);
                }

                if (string.IsNullOrWhiteSpace(propertyNameLiteral.Value))                    
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ObjectLiteralPropertyNameCannotBeNullEmptyOrWhitespaceAtPosition, _reader.Position);
                }

                _reader.ExpectAndConsume(ExpressionTokenCategory.LambdaSeparatorOrObjectLiteralPropertySeparator);

                if (!_atomParser.TryParse(context, out var propertyValue))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseObjectLiteralPropertyValueAtPosition, _reader.Position);
                }

                properties.Add(new ObjectLiteralPropertyExpression(propertyNameLiteral.Value, propertyValue));

                if (_reader.CurrentToken.Category == ExpressionTokenCategory.ParameterSeparator)
                {
                    _reader.ConsumeCurrent();
                    continue;
                }
            }

            _reader.ExpectAndConsume(ExpressionTokenCategory.EndOfObjectLiteral);

            expression = new ObjectLiteralExpression(properties);

            return true;
        }
    }
}
