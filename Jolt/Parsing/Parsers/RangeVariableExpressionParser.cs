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

            expression = new RangeVariableExpression(_reader.CurrentToken.Value, providesNullSafeAccess: category == ExpressionTokenCategory.NullSafeRangeVariableDereference).WithSpan(_reader.CurrentToken.Span);

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
            else if (IsPropertyDereference(_reader.CurrentToken) || _reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfIndexerOrArrayLiteral)
            {
                if (expression is RangeVariablePairExpression pair)
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToIndexOrSliceVariablePair, ((RangeVariableExpression)expression).Name, pair.SecondVariable.Name);
                }

                // The variable itself can be indexed or sliced before its properties are dereferenced (e.g. "@x[0].name").

                var variableIndexers = ParseIndexers(context, rangeExpressionParser);

                if (variableIndexers.Length == 1 && !IsPropertyDereference(_reader.CurrentToken))
                {
                    // A method can be piped from the indexed or sliced variable (e.g. "@x[0]?->#trim()"), just as from the variable itself.

                    expression = ParsePipedMethodCallsFrom(context, new SlicedVariableExpression((RangeVariableExpression)expression, variableIndexers[0]));

                    return true;
                }

                var dereferenceChain = new List<DereferenceExpression>();

                while (IsPropertyDereference(_reader.CurrentToken))
                {
                    var property = _reader.CurrentToken;

                    _reader.ConsumeCurrent();

                    // Any property in the chain can be indexed or sliced (e.g. "@x.items[0].name"), as many times as needed (e.g. "@x.rows[1][0]").

                    dereferenceChain.Add(new DereferenceExpression(property.Value, property.Category == ExpressionTokenCategory.NullSafePropertyDereference, ParseIndexers(context, rangeExpressionParser)));
                }

                expression = new PropertyDereferenceExpression((RangeVariableExpression)expression, dereferenceChain.ToArray(), variableIndexers);

                // A method can be piped from the dereferenced property (e.g. "@x.name->#trim()"), just as from the variable itself.

                if (MethodCallExpressionParser.IsStartOfPipe(_reader.CurrentToken))
                {
                    if (!methodCallParser.TryParse(context, out var pipedMethod, expression))
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCompleteParsingOfPipedMethodCall);
                    }

                    expression = pipedMethod;
                }

                return true;
            }
            else if (MethodCallExpressionParser.IsStartOfPipe(_reader.CurrentToken) && methodCallParser.TryParse(context, out var method, (RangeVariableExpression)expression))
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

            return true;
        }

        private static bool IsPropertyDereference(ExpressionToken? token)
        {
            return token?.Category == ExpressionTokenCategory.PropertyDereference || token?.Category == ExpressionTokenCategory.NullSafePropertyDereference;
        }

        /// <summary>
        /// Parses any indexes or slices in a row (e.g. the "[1][0]" in "@x.rows[1][0]").
        /// </summary>
        private RangeExpression[] ParseIndexers(IJsonContext context, RangeExpressionParser rangeExpressionParser)
        {
            var indexers = new List<RangeExpression>();

            while (_reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfIndexerOrArrayLiteral)
            {
                _reader.ConsumeCurrent();

                if (!rangeExpressionParser.TryParse(context, out var indexerExpression))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseIndexerExpressionAtPosition, _reader.Position);
                }

                if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.EndOfIndexerOrArrayLiteral))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCloseIndexerExpressionAtPosition, _reader.Position);
                }

                indexers.Add(indexerExpression as RangeExpression ?? throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedIndexOrSliceRangeButFoundOtherExpression, indexerExpression.GetType().Name));
            }

            return indexers.ToArray();
        }


    }
}
