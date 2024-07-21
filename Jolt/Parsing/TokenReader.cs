using Jolt.Exceptions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing
{
    public class TokenReader : ITokenReader
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

        public IEnumerable<ExpressionToken> ReadToEnd(string expression)
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
                else if (stream.CurrentToken == ExpressionToken.CloseParentheses)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.EndOfMethodCallAndParameters);
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

                    yield return TokenUntilEnd(stream, ExpressionTokenCategory.GeneratedNameIdentifier);
                }
                else if (stream.CurrentToken == ExpressionToken.SingleQuote)
                {
                    stream.ConsumeCurrent();

                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.StringLiteral, ExpressionToken.SingleQuote);

                    stream.ConsumeCurrent();
                }
                else if (stream.CurrentToken == ExpressionToken.DollarSign)
                {
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.PathLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses);
                }
                else if (char.IsNumber(stream.CurrentToken))
                {
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.NumericLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses);
                }
                else if (char.IsLetter(stream.CurrentToken))
                {
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.BooleanLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses);
                }
                else if (stream.CurrentToken == ExpressionToken.Whitespace)
                {
                    stream.ConsumeCurrent();
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

        private ExpressionToken TokenUntilMatchedWith(TokenStream<char> stream, ExpressionTokenCategory category, params char[] tokens)
        {
            if (!stream.TryConsumeUntil(x => tokens.Contains(x), out var consumedTokens))
            {
                var expectedCharacters = string.Join(", ", tokens);

                throw new JoltParsingException($"Unable to locate expected character(s) '{expectedCharacters}' in expression");
            }

            return new ExpressionToken(new string(consumedTokens), category);
        }
    }
}
