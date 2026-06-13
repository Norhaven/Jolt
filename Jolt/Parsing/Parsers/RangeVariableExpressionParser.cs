using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal sealed class RangeVariableExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.RangeVariable;

        public RangeVariableExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser) 
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.RangeVariable, ExpressionTokenCategory.NullSafeRangeVariableDereference);
        }

        private bool TryParseRangeVariable(IJsonContext context, out Expression? expression)
        {
            expression = default;

            var category = _reader.CurrentToken?.Category;

            if (category != ExpressionTokenCategory.RangeVariable && category != ExpressionTokenCategory.NullSafeRangeVariableDereference)
            {
                return false;
            }

            expression = new RangeVariableExpression(_reader.CurrentToken.Value, providesNullSafeAccess: category == ExpressionTokenCategory.NullSafeRangeVariableDereference);

            _reader.ConsumeCurrent();

            return true;
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (TryParseRangeVariable(context, out expression))
            {
                // Is this a variable pair or just a single one?

                if (TryParseRangeVariable(context, out var secondVariable))
                {
                    expression = new RangeVariablePairExpression((RangeVariableExpression)expression, (RangeVariableExpression)secondVariable);
                }
            }
            else
            {
                return false;
            }

            var rangeVariableParser = GetExpressionParserOf<RangeVariableExpressionParser>();
            var rangeExpressionParser = GetExpressionParserOf<RangeExpressionParser>();
            var methodCallParser = GetExpressionParserOf<MethodCallExpressionParser>();
            var literalParser = GetExpressionParserOf<LiteralExpressionParser>();

            if (_reader.CurrentToken?.Category == ExpressionTokenCategory.In)
            {
                _reader.ConsumeCurrent();

                if (!_atomParser.TryParse(context, out var enumerationSource))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseEnumerationSourceForVariable, ((RangeVariableExpression)expression).Name);
                }

                expression = new EnumerateAsVariableExpression((RangeVariableExpression)expression, enumerationSource);

                return true;
            }
            else if (_reader.CurrentToken?.Category == ExpressionTokenCategory.LambdaSeparatorOrObjectLiteralPropertySeparator)
            {
                _reader.ConsumeCurrent();

                if (!_atomParser.TryParse(context, out var bodyExpression))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseLambdaExpressionBodyAtPosition, _reader.Position, _reader.CurrentToken);
                }

                expression = new LambdaMethodExpression((RangeVariableExpression)expression, bodyExpression);

                return true;
            }
            else if (_reader.CurrentToken?.Category == ExpressionTokenCategory.PropertyDereference || _reader.CurrentToken?.Category == ExpressionTokenCategory.NullSafePropertyDereference)
            {
                while (_reader.CurrentToken.Category == ExpressionTokenCategory.PropertyDereference || _reader.CurrentToken.Category == ExpressionTokenCategory.NullSafePropertyDereference)
                {
                    if (!_reader.TryConsumeUntilMatchOrEnd(x => x.Category != ExpressionTokenCategory.PropertyDereference && x.Category != ExpressionTokenCategory.NullSafePropertyDereference, out var dereferenceChain))
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParsePropertyDereferenceChain, _reader.CurrentToken.Value);
                    }

                    expression = new PropertyDereferenceExpression((RangeVariableExpression)expression, dereferenceChain.Select(x => new DereferenceExpression(x.Value, x.Category == ExpressionTokenCategory.NullSafePropertyDereference)).ToArray());

                    return true;
                }
            }
            else if (_reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfPipedMethodCall && methodCallParser.TryParse(context, out var method, (RangeVariableExpression)expression))
            {
                expression = method;

                return true;
            }
            else if (_reader.CurrentToken?.Category == ExpressionTokenCategory.As)
            {
                _reader.ConsumeCurrent();

                if (!rangeVariableParser.TryParse(context, out var aliasVariable))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseVariableAlias);
                }

                expression = new VariableAliasExpression((RangeVariableExpression)expression, (RangeVariableExpression)aliasVariable);

                return true;
            }
            else if (_reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfIndexerOrArrayLiteral)
            {
                if (expression is RangeVariablePairExpression pair)
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToIndexOrSliceVariablePair, ((RangeVariableExpression)expression).Name, pair.SecondVariable.Name);
                }

                _reader.ConsumeCurrent();

                if (!rangeExpressionParser.TryParse(context, out var indexerExpression))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseIndexerExpressionAtPosition, _reader.Position);
                }

                if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.EndOfIndexerOrArrayLiteral))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCloseIndexerExpressionAtPosition, _reader.Position);
                }

                if (indexerExpression is RangeExpression parsedRange)
                {
                    expression = new SlicedVariableExpression((RangeVariableExpression)expression, parsedRange);

                    return true;
                }

                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedIndexOrSliceRangeButFoundOtherExpression, indexerExpression.GetType().Name);
            }

            return true;
        }


    }
}
