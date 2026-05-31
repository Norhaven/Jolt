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
    internal sealed class MethodCallExpressionParser : SpecializedExpressionParser
    {
        public override AtomType AtomType => AtomType.MethodCall;

        public MethodCallExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser)
            : base(reader, atomParser)
        {
        }

        public override bool CanParse(IJsonContext context)
        {
            return IsInCategory(ExpressionTokenCategory.StartOfMethodCall, ExpressionTokenCategory.StartOfPipedMethodCall);
        }

        public override bool TryParse(IJsonContext context, out Expression? expression)
        {
            expression = default;

            if (!TryParse(context, out var methodCallExpression, default))
            {
                return false;
            }

            expression = methodCallExpression;

            return true;
        }

        private Expression TryReduce(IJsonContext context, Expression expression)
        {   
            if (_reader.IsCategory(ExpressionTokenCategory.CloseParenthesesGroup))
            {
                return expression;
            }

            if (_reader.IsCategory(ExpressionTokenCategory.RangeExpressionOperator))
            {
                if (expression is LiteralExpression literal && literal.Type == typeof(long))
                {
                    if (_atomParser.TryParse(context, out var rangeExpression) && rangeExpression is RangeExpression range)
                    {
                        return new RangeExpression(new RangeIndexExpression(expression, false), range.EndIndex);
                    }

                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToReduceLiteralFollowedByRangeExpression, literal.Value);
                }
                else if (expression is RangeVariableExpression variable)
                {
                    if (_atomParser.TryParse(context, out var rangeExpression) && rangeExpression is RangeExpression range)
                    {
                        return new RangeExpression(new RangeIndexExpression(expression, false), range.EndIndex);
                    }

                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToReduceRangeVariableFollowedByRangeExpression, variable.Name);
                }
                else if (expression is MethodCallExpression methodCall)
                {
                    if (_atomParser.TryParse(context, out var rangeExpression) && rangeExpression is RangeExpression range)
                    {
                        return new RangeExpression(new RangeIndexExpression(expression, false), range.EndIndex);
                    }

                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToReduceMethodCallFollowedByRangeExpression, methodCall.Signature.Name);
                }
            }

            return expression;
        }

        public bool TryParse(IJsonContext context, out MethodCallExpression? methodCall, Expression? invocationSource)
        {
            methodCall = default;

            if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodCall ||
                                                     x.Category == ExpressionTokenCategory.StartOfPipedMethodCall))
            {
                return false;
            }

            if (!_reader.TryConsumeNext(out var potentiallyQualifiedMethodName))
            {
                return false;
            }

            if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfMethodParameters))
            {
                return false;
            }

            // If the method call was initiated off of a range variable, we need to include that as the first parameter
            // so it doesn't get lost in the shuffle and give us a parameter count mismatch.

            var actualParameters = invocationSource != null ? new List<Expression> { invocationSource } : new List<Expression>();

            if (_reader.CurrentToken.Category != ExpressionTokenCategory.CloseParenthesesGroup)
            {
                do
                {
                    if (!_atomParser.TryParse(context, out var actualValue))
                    {
                        return false;
                    }

                    actualValue = TryReduce(context, actualValue);

                    actualParameters.Add(actualValue);

                    if (_reader.IsCategory(ExpressionTokenCategory.CloseParenthesesGroup))
                    {
                        break;
                    }
                }
                while (_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.ParameterSeparator));
            }

            if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.CloseParenthesesGroup))
            {
                return false;
            }

            var methodSignature = context.ReferenceResolver.GetMethod(potentiallyQualifiedMethodName.Value);

            if (methodSignature is null)
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToFindMethodImplementation, potentiallyQualifiedMethodName.Value);
            }

            if (_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.GeneratedNameIdentifier, out var generatedName))
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), generatedName.Value);
            }
            else if (_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.RangeVariable, out var rangeVariable))
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), rangeVariable.Value, new RangeVariable(rangeVariable.Value));
            }
            else if (_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.StartOfIndexerOrArrayLiteral, out var indexer))
            {                
                var rangeExpressionParser = _atomParser.GetExpressionParserOf<RangeExpressionParser>();

                if (!rangeExpressionParser.TryParse(context, out var range))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.ExpectedIndexOrSliceRangeButFoundOtherExpression, _reader.CurrentToken.Value);
                }

                if (!_reader.TryMatchNextAndConsume(x => x.Category == ExpressionTokenCategory.EndOfIndexerOrArrayLiteral))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCloseIndexerExpressionAtPosition, _reader.Position);
                }

                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray());
                methodCall = new IndexOrSliceMethodResultExpression((RangeExpression)range, methodCall);

                if (_reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfPipedMethodCall)
                {
                    if (!TryParse(context, out var pipedMethodCall, methodCall))
                    {
                        throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCompleteParsingOfPipedMethodCall);
                    }

                    methodCall = pipedMethodCall;

                    return true;
                }
            }
            else if (_reader.CurrentToken?.Category == ExpressionTokenCategory.StartOfPipedMethodCall)
            {
                if (!TryParse(context, out var pipedMethodCall))
                {
                    throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCompleteParsingOfPipedMethodCall);
                }

                // We're piping the initial method call results into the first argument of the target method,
                // so we need to switch the evaluation order around a little bit.

                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray(), default);

                var leftMostMethodCall = (MethodCallExpression)pipedMethodCall;
                MethodCallExpression? previousCall = default;

                while (leftMostMethodCall.ParameterValues.Length > 0)
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
                    methodCall = (MethodCallExpression)pipedMethodCall;
                }
            }
            else
            {
                methodCall = new MethodCallExpression(methodSignature, actualParameters.ToArray());
            }

            return true;
        }
    }
}
