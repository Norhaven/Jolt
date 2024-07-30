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
        public bool StartsWithMethodCall(string expression)
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

            return stream.CurrentToken == ExpressionToken.Hash;
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
                if (stream.CurrentToken == ExpressionToken.Hash)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodCall);
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.Identifier, ExpressionToken.OpenParentheses);
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodParameters);
                }
                else if (stream.CurrentToken == ExpressionToken.OpenParentheses)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.OpenParenthesesGroup);
                }
                else if (stream.CurrentToken == ExpressionToken.CloseParentheses)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.CloseParenthesesGroup);
                }
                else if (stream.CurrentToken == ExpressionToken.Comma)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.ParameterSeparator);
                }
                else if (stream.CurrentToken == ExpressionToken.ArrowBody)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken != ExpressionToken.ArrowHead)
                    {
                        throw new JoltParsingException($"Found token '-' and expected '>' at position '{stream.Position}' but found '{stream.CurrentToken}'.");
                    }

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
                            throw new JoltParsingException($"Expected a '#' character to begin a piped method but found character '{stream.CurrentToken}' at position '{stream.Position}'");
                        }

                        if (stream.CurrentToken == ExpressionToken.SingleQuote)
                        {
                            stream.ConsumeCurrent();

                            yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.GeneratedNameIdentifier, ExpressionToken.SingleQuote);
                        }
                        else
                        {
                            throw new JoltParsingException($"The property key must resolve to a string literal surrounded with single quote marks, but found token '{stream.CurrentToken}' instead");
                        }
                    }
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
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.BooleanLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace);
                }
                else if (stream.CurrentToken == ExpressionToken.Whitespace)
                {
                    stream.ConsumeCurrent();
                }
                else if (stream.CurrentToken == ExpressionToken.Equal)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.EqualComparison);
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
                else if (stream.CurrentToken == ExpressionToken.Minus)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.Subtraction);
                }
                else if (stream.CurrentToken == ExpressionToken.Star)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.Multiplication);
                }
                else if (stream.CurrentToken == ExpressionToken.ForwardSlash)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.Division);
                }
                else if (stream.CurrentToken == ExpressionToken.Not)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken == ExpressionToken.Equal)
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.NotEqualComparison);
                    }
                }
                else
                {
                    throw new JoltParsingException($"Unable to categorize token '{stream.CurrentToken}' at position '{stream.Position}'.");
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
                    throw new JoltParsingException($"Unable to locate expected character(s) in expression");
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

                    throw new JoltParsingException($"Unable to locate expected character(s) '{expectedCharacters}' in expression");
                }
            }

            return new ExpressionToken(new string(consumedTokens), category);
        }
    }
}
