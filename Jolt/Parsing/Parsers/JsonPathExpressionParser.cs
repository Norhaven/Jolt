using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class JsonPathExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.JsonPath;

        public JsonPathExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser)
            : base(reader, atomParser)
        {

        }

        public override bool CanParse(IJsonContext context) => _reader.IsCategory(ExpressionTokenCategory.PathLiteral);

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = _reader.CurrentToken.Category switch
            {
                ExpressionTokenCategory.PathLiteral => new PathExpression(_reader.CurrentToken.Value),
                _ => default
            };

            var isPath = expression != null;

            if (isPath)
            {
                _reader.ConsumeCurrent();

                if (_reader.CurrentToken?.Category == ExpressionTokenCategory.As)
                {
                    _reader.ConsumeCurrent();

                    var rangeVariableParser = GetExpressionParserOf<RangeVariableExpressionParser>();

                    if (!rangeVariableParser.TryParse(context, out var aliasVariable))
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseVariableAlias);
                    }

                    expression = new VariableAliasExpression((PathExpression)expression, (RangeVariableExpression)aliasVariable);
                }
            }

            return isPath;
        }
    }
}
