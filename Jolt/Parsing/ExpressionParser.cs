using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Parsing
{
    public sealed class ExpressionParser : IExpressionParser
    {
        private sealed class ExpressionReader : TokenStream<ExpressionToken>
        {
            private readonly IJsonContext _context;

            public ExpressionReader(IEnumerable<ExpressionToken> tokens, IJsonContext context) 
                : base(tokens)
            {
                _context = context;
            }

            public bool ExpectAndConsume(ExpressionTokenCategory category)
            {
                if (IsCompleted)
                {
                    throw _context.CreateParsingErrorFor<ExpressionReader>(ExceptionCode.ExpectedTokenButFoundEndOfExpression, category.GetDescription());
                }

                if (CurrentToken.Category != category)
                {
                    throw _context.CreateParsingErrorFor<ExpressionReader>(ExceptionCode.ExpectedTokenButFoundDifferentToken, category.GetDescription(), CurrentToken.Category.GetDescription());
                }

                return true;
            }

            public bool IsCategory(ExpressionTokenCategory category)
            {
                if (IsCompleted)
                {
                    throw _context.CreateParsingErrorFor<ExpressionReader>(ExceptionCode.ExpectedTokenButFoundEndOfExpression, category.GetDescription());
                }

                return CurrentToken.Category == category;
            }
        }

        public bool TryParseExpression(IEnumerable<ExpressionToken> tokens, IJsonContext context, out Expression? expression) => TryParseExpression(new ExpressionReader(tokens, context), context, out expression);
               
        private bool TryParseExpression(ExpressionReader reader, IJsonContext context, out Expression expression)
        {
            Expression ReadNextAtom(ExpressionReader reader)
            {
                if (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.OpenParenthesesGroup))
                {
                    if (!TryParseExpression(reader, context, out var parenthesizedExpression))
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseParenthesizedExpressionAtPosition, reader.Position);
                    }

                    if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.CloseParenthesesGroup))
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCloseParenthesizedExpressionAtPosition, reader.Position);
                    }

                    return parenthesizedExpression;
                }
                else if (TryParseMethod(reader, context, out var method))
                {
                    return method;
                }
                else if (TryParsePath(reader, out var path))
                {
                    if (reader.CurrentToken?.Category == ExpressionTokenCategory.As)
                    {
                        reader.ConsumeCurrent();

                        if (!TryParseRangeVariable(reader, out var aliasVariable))
                        {
                            throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseVariableAlias);
                        }

                        return new VariableAliasExpression(path, aliasVariable);
                    }

                    return path;
                }
                else if (TryParseRangeExpression(reader, context, out var range))
                {
                    return range;
                }
                else if (TryParseRangeVariable(reader, out var rangeVariable))
                {
                    if (TryParseRangeVariable(reader, out var secondRangeVariable))
                    {
                        rangeVariable = new RangeVariablePairExpression(rangeVariable, secondRangeVariable);
                    }

                    if (reader.CurrentToken?.Category == ExpressionTokenCategory.LambdaSeparator)
                    {
                        reader.ConsumeCurrent();

                        if (!TryParseExpression(reader, context, out var bodyExpression))
                        {
                            throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseLambdaExpressionBodyAtPosition, reader.Position, reader.CurrentToken);
                        }

                        return new LambdaMethodExpression(rangeVariable, bodyExpression);
                    }
                    else if (reader.CurrentToken?.Category == ExpressionTokenCategory.PropertyDereference)
                    {
                        if (!reader.TryConsumeUntilMatchOrEnd(x => x.Category != ExpressionTokenCategory.PropertyDereference, out var dereferenceChain))
                        {
                            throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParsePropertyDereferenceChain, reader.CurrentToken.Value);
                        }

                        return new PropertyDereferenceExpression(rangeVariable, dereferenceChain.Select(x => x.Value).ToArray());
                    }
                    else if (reader.CurrentToken?.Category == ExpressionTokenCategory.In)
                    {
                        reader.ConsumeCurrent();

                        if (!TryParseExpression(reader, context, out var enumerationSource))
                        {
                            throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseEnumerationSourceForVariable, rangeVariable.Name);
                        }

                        return new EnumerateAsVariableExpression(rangeVariable, enumerationSource);
                    }
                    else if (reader.CurrentToken?.Category == ExpressionTokenCategory.As)
                    {
                        reader.ConsumeCurrent();

                        if (!TryParseRangeVariable(reader, out var aliasVariable))
                        {
                            throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseVariableAlias);
                        }

                        return new VariableAliasExpression(rangeVariable, aliasVariable);
                    }

                    return rangeVariable;
                }
                else if (TryParseLiteral(reader, out var literal))
                {
                    return literal;
                }

                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseExpressionStartingWithDescription, reader.CurrentToken.Category.GetDescription());
            }

            int GetOperatorPrecedence(Operator @operator)
            {
                return @operator switch
                {
                    Operator.Equal => 0,
                    Operator.NotEqual => 0,
                    Operator.LessThan => 0,
                    Operator.GreaterThan => 0,
                    Operator.LessThanOrEquals => 0,
                    Operator.GreaterThanOrEquals => 0,
                    Operator.Addition => 1,
                    Operator.Subtraction => 1,
                    Operator.Multiplication => 2,
                    Operator.Division => 2,
                    _ => -1
                };
            }

            Expression ParsePrecedenceExpression(ExpressionReader reader, Expression leftExpression, int minimumPrecedence)
            {
                var lookahead = ToOperator(reader.CurrentToken);
                var lookaheadPrecedence = GetOperatorPrecedence(lookahead);

                while(lookaheadPrecedence >= minimumPrecedence)
                {
                    var @operator = lookahead;
                    var operatorPrecedence = GetOperatorPrecedence(@operator);

                    reader.ConsumeCurrent();

                    var rightExpression = ReadNextAtom(reader);

                    lookahead = ToOperator(reader.CurrentToken);
                    lookaheadPrecedence = GetOperatorPrecedence(lookahead);

                    while(lookaheadPrecedence > operatorPrecedence)
                    {
                        var adjustedMinimumPrecedence = operatorPrecedence + (lookaheadPrecedence > operatorPrecedence ? 1 : 0);

                        rightExpression = ParsePrecedenceExpression(reader, rightExpression, adjustedMinimumPrecedence);

                        lookahead = ToOperator(reader.CurrentToken);
                        lookaheadPrecedence = GetOperatorPrecedence(lookahead);
                    }

                    leftExpression = new BinaryExpression(leftExpression, @operator, rightExpression);
                }

                return leftExpression;
            }

            Operator ToOperator(ExpressionToken token)
            {
                return token?.Category switch
                {
                    ExpressionTokenCategory.EqualComparison => Operator.Equal,
                    ExpressionTokenCategory.GreaterThanComparison => Operator.GreaterThan,
                    ExpressionTokenCategory.LessThanComparison => Operator.LessThan,
                    ExpressionTokenCategory.GreaterThanOrEqualComparison => Operator.GreaterThanOrEquals,
                    ExpressionTokenCategory.LessThanOrEqualComparison => Operator.LessThanOrEquals,
                    ExpressionTokenCategory.Addition => Operator.Addition,
                    ExpressionTokenCategory.Subtraction => Operator.Subtraction,
                    ExpressionTokenCategory.Multiplication => Operator.Multiplication,
                    ExpressionTokenCategory.Division => Operator.Division,
                    ExpressionTokenCategory.NotEqualComparison => Operator.NotEqual,
                    _ => Operator.Unknown
                };
            }

            var leftExpression = ReadNextAtom(reader);

            expression = ParsePrecedenceExpression(reader, leftExpression, 0);

            return true;
        }

        private bool TryParseRangeExpression(ExpressionReader reader, IJsonContext context, out RangeExpression? range)
        {
            const string RangeDots = "..";

            range = default;
            
            if (reader.CurrentToken.Category != ExpressionTokenCategory.NumericLiteral)
            {
                return false;
            }

            var value = reader.CurrentToken.Value;

            if (!value.Contains(RangeDots) && !value.Contains(ExpressionToken.RangeEndIndexer))
            {
                return false;
            }

            var pieces = value.Split(RangeDots, StringSplitOptions.RemoveEmptyEntries);

            if (pieces.Length > 2)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToUseMultipleRangeOperatorsWithinSameExpression);
            }

            bool IsEndIndexerUsedAt(int index) => pieces[index].StartsWith(ExpressionToken.RangeEndIndexer);
            Index ParseWithOptionalEndIndexerAt(int index) => IsEndIndexerUsedAt(index) ? new Index(int.Parse(pieces[index][1..]), true) : new Index(int.Parse(pieces[index]));

            var startIndex = value switch
            {
                var x when x.StartsWith(RangeDots) => new Index(0), 
                _ => ParseWithOptionalEndIndexerAt(0)
            };

            var endIndex = value switch
            {
                var x when x.EndsWith(RangeDots) => new Index(0, true),
                var _ when pieces.Length == 1 => ParseWithOptionalEndIndexerAt(0),
                var _ when pieces.Length == 2 => ParseWithOptionalEndIndexerAt(1),
                _ => throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToParseRangeExpressionFormatIsInvalid, value)
            };

            range = new RangeExpression(startIndex, endIndex);

            reader.ConsumeCurrent();

            return true;
        }

        private bool TryParseRangeVariable(ExpressionReader reader, out RangeVariableExpression rangeVariable)
        {
            rangeVariable = default;

            if (reader.CurrentToken?.Category != ExpressionTokenCategory.RangeVariable)
            {
                return false;
            }

            rangeVariable = new RangeVariableExpression(reader.CurrentToken.Value);

            reader.ConsumeCurrent();

            return true;
        }

        private bool TryParseLiteral(ExpressionReader reader, out LiteralExpression? literal)
        {
            literal = reader.CurrentToken.Category switch
            { 
                ExpressionTokenCategory.NumericLiteral when reader.CurrentToken.Value.ToString().Contains('.') => new LiteralExpression(typeof(double), reader.CurrentToken.Value),
                ExpressionTokenCategory.NumericLiteral => new LiteralExpression(typeof(long), reader.CurrentToken.Value),
                ExpressionTokenCategory.BooleanLiteral => new LiteralExpression(typeof(bool), reader.CurrentToken.Value),
                ExpressionTokenCategory.StringLiteral => new LiteralExpression(typeof(string), reader.CurrentToken.Value),
                _ => default
            };

            var isParseSuccessful = literal != null;

            if (isParseSuccessful)
            {
                reader.ConsumeCurrent();
            }

            return isParseSuccessful;
        }

        private bool TryParseMethod(ExpressionReader reader, IJsonContext context, out MethodCallExpression? methodCall)
        {
            methodCall = default;

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodCall || 
                                                    x.Category == ExpressionTokenCategory.StartOfPipedMethodCall))
            {
                return false;
            }

            if (!reader.TryConsumeNext(out var potentiallyQualifiedMethodName))
            {
                return false;
            }

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodParameters))
            {
                return false;
            }

            var actualParameters = new List<Expression>();

            if (reader.CurrentToken.Category != ExpressionTokenCategory.CloseParenthesesGroup)
            {
                do
                {
                    if (!TryParseExpression(reader, context, out var actualValue))
                    {
                        return false;
                    }

                    actualParameters.Add(actualValue);

                    if (reader.IsCategory(ExpressionTokenCategory.CloseParenthesesGroup))
                    {
                        break;
                    }
                }
                while (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.ParameterSeparator));
            }

            if (!reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.CloseParenthesesGroup))
            {
                return false;
            }            

            var methodSignature = context.ReferenceResolver.GetMethod(potentiallyQualifiedMethodName.Value);

            if (methodSignature is null)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToFindMethodImplementation, potentiallyQualifiedMethodName.Value);
            }

            if (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.GeneratedNameIdentifier, out var generatedName))
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), generatedName.Value);
            }
            else if (reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.RangeVariable, out var rangeVariable))
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), rangeVariable.Value, new RangeVariable(rangeVariable.Value));
            }
            else if (reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfPipedMethodCall)
            {
                if (!TryParseMethod(reader, context, out var pipedMethodCall))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCompleteParsingOfPipedMethodCall);
                }

                // We're piping the initial method call results into the first argument of the target method,
                // so we need to switch the evaluation order around a little bit.

                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), default);

                var leftMostMethodCall = pipedMethodCall;
                MethodCallExpression? previousCall = default;

                while(leftMostMethodCall.ParameterValues.Length > 0)
                {
                    if (!(leftMostMethodCall.ParameterValues[0] is MethodCallExpression next))
                    {
                        break;
                    }

                    previousCall = leftMostMethodCall;
                    leftMostMethodCall = next;
                }

                var updatedParameters = new[] { methodCall }.Concat(leftMostMethodCall.ParameterValues);

                var updatedCall = leftMostMethodCall.WithParameters(updatedParameters);

                if (previousCall is null)
                {
                    methodCall = updatedCall;
                }
                else
                {
                    previousCall.ParameterValues[0] = updatedCall;
                    methodCall = pipedMethodCall;
                }
            }
            else
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray());
            }

            return true;
        }

        private bool TryParsePath(ExpressionReader reader, out PathExpression? path)
        {
            path = reader.CurrentToken.Category switch
            {
                ExpressionTokenCategory.PathLiteral => new PathExpression(reader.CurrentToken.Value),
                _ => default
            };

            var isPath = path != null;

            if (isPath)
            {
                reader.ConsumeCurrent();
            }

            return isPath;
        }
    }
}
