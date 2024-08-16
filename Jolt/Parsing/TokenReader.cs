using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing
{
    public sealed class TokenReader : ITokenReader
    {
        public bool StartsWithMethodCallOrOpenParenthesesOrRangeVariable(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            var stream = new TokenStream<char>(expression);

            if (stream.IsCompleted)
            {
                return false;
            }

            while(stream.CurrentToken == ExpressionToken.Whitespace)
            {
                stream.ConsumeCurrent();
            }

            return stream.CurrentToken == ExpressionToken.Hash || 
                   stream.CurrentToken == ExpressionToken.OpenParentheses || 
                   stream.CurrentToken == ExpressionToken.At;
        }

        public IEnumerable<ExpressionToken> ReadToEnd(string expression, EvaluationMode mode)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                yield break;
            }

            var stream = new TokenStream<char>(expression);

            while(!stream.IsCompleted)
            {
                if (stream.CurrentToken == ExpressionToken.OpenParentheses)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.OpenParenthesesGroup);
                }
                else if (stream.CurrentToken == ExpressionToken.CloseParentheses)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.CloseParenthesesGroup);
                }
                else if (stream.CurrentToken == ExpressionToken.Equal)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.EqualComparison);
                }
                else if (stream.CurrentToken == ExpressionToken.Not)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken == ExpressionToken.Equal)
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.NotEqualComparison);
                    }
                }
                else if (stream.CurrentToken == ExpressionToken.LessThan)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken == ExpressionToken.Equal)
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.LessThanOrEqualComparison);
                    }
                    else
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.LessThanComparison);
                    }
                }
                else if (stream.CurrentToken == ExpressionToken.GreaterThan)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken == ExpressionToken.Equal)
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.GreaterThanOrEqualComparison);
                    }
                    else
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.GreaterThanComparison);
                    }
                }
                else if (stream.CurrentToken == ExpressionToken.Plus)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.Addition);
                }
                else if (stream.CurrentToken == ExpressionToken.Star)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.Multiplication);
                }
                else if (stream.CurrentToken == ExpressionToken.ForwardSlash)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.Division);
                }
                else if (stream.CurrentToken == ExpressionToken.ArrowBody || stream.CurrentToken == ExpressionToken.Minus)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken == ExpressionToken.ArrowHead)
                    {
                        stream.ConsumeCurrent();

                        // This could be a piped method call (if on the value side) or a
                        // generated property name (if on the key side), so let's figure it out here.

                        if (stream.CurrentToken == ExpressionToken.Hash)
                        {
                            yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfPipedMethodCall);
                            yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.Identifier, ExpressionToken.OpenParentheses);
                            yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodParameters);
                        }
                        else
                        {
                            if (mode == EvaluationMode.PropertyValue)
                            {
                                throw Error.CreateParsingErrorFrom(ExceptionCode.ExpectedStartOfMethodCallButFoundDifferentTokenAtPosition, stream.CurrentToken, stream.Position);
                            }

                            if (stream.CurrentToken == ExpressionToken.SingleQuote)
                            {
                                stream.ConsumeCurrent();

                                yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.GeneratedNameIdentifier, ExpressionToken.SingleQuote);
                            }
                            else if (stream.CurrentToken == ExpressionToken.At)
                            {
                                yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.RangeVariable, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace);
                            }
                            else
                            {
                                throw Error.CreateParsingErrorFrom(ExceptionCode.ExpectedStringLiteralPropertyNameButFoundDifferentToken, stream.CurrentToken);
                            }
                        }
                    }
                    else
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.Subtraction);
                    }
                }
                else if (stream.CurrentToken == ExpressionToken.Hash)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodCall);
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.Identifier, ExpressionToken.OpenParentheses);
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodParameters);
                }                
                else if (stream.CurrentToken == ExpressionToken.At)
                {
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.RangeVariable, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace, ExpressionToken.Colon, ExpressionToken.Dot);

                    while (stream.CurrentToken == ExpressionToken.Dot)
                    {
                        stream.ConsumeCurrent();

                        yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.PropertyDereference, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace, ExpressionToken.Colon, ExpressionToken.Dot);
                    }
                }
                else if (stream.CurrentToken == ExpressionToken.Colon)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.LambdaSeparator);
                }
                else if (stream.CurrentToken == ExpressionToken.Comma)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.ParameterSeparator);
                }                
                else if (stream.CurrentToken == ExpressionToken.SingleQuote)
                {
                    stream.ConsumeCurrent();

                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.StringLiteral, ExpressionToken.SingleQuote);

                    stream.ConsumeCurrent();
                }
                else if (stream.CurrentToken == ExpressionToken.DollarSign)
                {
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.PathLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace);
                }
                else if (char.IsNumber(stream.CurrentToken) || stream.CurrentToken == ExpressionToken.RangeEndIndexer || stream.CurrentToken == ExpressionToken.DecimalPoint)
                {
                    yield return TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.NumericLiteral, x => char.IsNumber(x) || x == ExpressionToken.DecimalPoint || x == ExpressionToken.RangeEndIndexer);
                }
                else if (char.IsLetter(stream.CurrentToken))
                {
                    var possibleBoolToken = TokenUntilMatchedWith(stream, ExpressionTokenCategory.BooleanLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace);

                    if (bool.TryParse(possibleBoolToken.Value, out var value))
                    {
                        yield return possibleBoolToken;
                    }
                    else
                    {
                        throw Error.CreateParsingErrorFrom(ExceptionCode.ExpectedBooleanLiteralTokenButFoundUnknownToken, possibleBoolToken.Value);
                    }
                }
                else if (stream.CurrentToken == ExpressionToken.Whitespace)
                {
                    stream.ConsumeCurrent();
                }                
                else
                {
                    throw Error.CreateParsingErrorFrom(ExceptionCode.UnableToCategorizeTokenAtPosition, stream.CurrentToken, stream.Position);
                }
            }
        }

        private ExpressionToken TokenUntilEnd(TokenStream<char> stream, ExpressionTokenCategory category)
        {
            var tokens = stream.ConsumeUntilEnd();

            return new ExpressionToken(new string(tokens), category);
        }

        private ExpressionToken TokenFromCurrent(TokenStream<char> stream, ExpressionTokenCategory category)
        {
            return new ExpressionToken(stream.ConsumeCurrent().ToString(), category);
        }

        private ExpressionToken TokenUntilNotMatchedWith(TokenStream<char> stream, ExpressionTokenCategory category, Func<char, bool> isMatch)
        {
            if (!stream.TryConsumeUntil(x => !isMatch(x), out var consumedTokens))
            {
                if (!stream.IsCompleted)
                {
                    throw Error.CreateParsingErrorFrom(ExceptionCode.UnableToLocateExpectedCharactersInExpression);
                }
            }

            return new ExpressionToken(new string(consumedTokens), category);
        }

        private ExpressionToken TokenUntilMatchedWith(TokenStream<char> stream, ExpressionTokenCategory category, params char[] tokens)
        {
            if (!stream.TryConsumeUntil(x => tokens.Contains(x), out var consumedTokens))
            {
                if (!stream.IsCompleted)
                {
                    var expectedCharacters = string.Join(", ", tokens);

                    throw Error.CreateParsingErrorFrom(ExceptionCode.UnableToLocateSpecificExpectedCharactersInExpression, expectedCharacters);
                }
            }

            return new ExpressionToken(new string(consumedTokens), category);
        }
    }
}
