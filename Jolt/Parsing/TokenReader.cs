using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Extensions;
using Jolt.Parsing.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing
{
    public sealed class TokenReader : BasicTokenReader, ITokenReader
    {
        public TokenReader(IMessageProvider messageProvider)
            : base(messageProvider)
        {
        }

        public bool StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracket(string expression)
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

            while (stream.CurrentToken == ExpressionToken.Whitespace)
            {
                stream.ConsumeCurrent();
            }

            return stream.CurrentToken == ExpressionToken.Hash ||
                   stream.CurrentToken == ExpressionToken.OpenParentheses ||
                   stream.CurrentToken == ExpressionToken.At ||
                   stream.CurrentToken == ExpressionToken.OpenSquareBracket;
        }

        public IEnumerable<ExpressionToken> ReadToEnd(string expression, EvaluationMode mode)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                yield break;
            }

            var stream = new TokenStream<char>(expression);

            while (!stream.IsCompleted)
            {
                foreach(var token in ReadTokenFrom(stream, mode))
                {
                    yield return token;
                }
            }
        }

        public override IEnumerable<ExpressionToken> ReadTokenFrom(ITokenStream<char> stream, EvaluationMode mode)
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
                stream.ConsumeCurrent();

                if (stream.CurrentToken != ExpressionToken.Equal)
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedDoubleEqualForEqualityComparisonButFoundSingleEqual);
                }

                stream.ConsumeCurrent();

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
            else if (stream.CurrentToken == ExpressionToken.And)
            {
                stream.ConsumeCurrent();

                if (stream.CurrentToken == ExpressionToken.And)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.LogicalAnd);
                }
                else
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedLogicalAndOperatorButFoundSingleAmpersand);
                }
            }
            else if (stream.CurrentToken == ExpressionToken.Or)
            {
                stream.ConsumeCurrent();

                if (stream.CurrentToken == ExpressionToken.Or)
                {
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.LogicalOr);
                }
                else
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedLogicalOrOperatorButFoundSinglePipe);
                }
            }
            else if (stream.CurrentToken == ExpressionToken.OpenSquareBracket)
            {
                yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfIndexerOrArrayLiteral);

                var operations = new List<ExpressionToken[]>();

                while (stream.CurrentToken != ExpressionToken.CloseSquareBracket)
                {
                    foreach(var token in ReadTokenFrom(stream, mode))
                    {
                        yield return token;
                    }
                }
                
                yield return TokenFromCurrent(stream, ExpressionTokenCategory.EndOfIndexerOrArrayLiteral);
            }
            else if (stream.CurrentToken == ExpressionToken.ArrowBody || stream.CurrentToken == ExpressionToken.Minus)
            {
                var minusToken = TokenFromCurrent(stream, ExpressionTokenCategory.Subtraction);

                if (stream.CurrentToken == ExpressionToken.ArrowHead)
                {
                    stream.ConsumeCurrent();

                    // This should be a piped method call (if on the value side) or else that's a problem.

                    if (stream.CurrentToken == ExpressionToken.Hash)
                    {
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfPipedMethodCall);
                        yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.Identifier, ExpressionToken.OpenParentheses);
                        yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodParameters);
                    }        
                    else
                    {
                        throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedPipedMethodCallAfterArrowOperatorButFoundDifferentToken, stream.CurrentToken);
                    }
                }
                else
                {
                    yield return minusToken;
                }
            }
            else if (stream.CurrentToken == ExpressionToken.Hash)
            {
                yield return TokenFromCurrent(stream, ExpressionTokenCategory.StartOfMethodCall);
                yield return TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.Identifier, x => char.IsLetterOrDigit(x));
                yield return TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.StartOfMethodParameters, x => x == ExpressionToken.OpenParentheses);
            }
            else if (stream.CurrentToken == ExpressionToken.At)
            {
                var variable = TokenUntilMatchedWith(stream, ExpressionTokenCategory.RangeVariable, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace, ExpressionToken.Colon, ExpressionToken.Semicolon, ExpressionToken.Dot, ExpressionToken.ArrowBody, ExpressionToken.OpenSquareBracket, ExpressionToken.CloseSquareBracket, ExpressionToken.QuestionMark);

                var isNullSafeVariableDereference = stream.CurrentToken == ExpressionToken.QuestionMark;

                if (isNullSafeVariableDereference)
                {
                    stream.ConsumeCurrent();

                    if (stream.CurrentToken != ExpressionToken.Dot)
                    {
                        throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedDotAfterQuestionMarkForNullSafePropertyAccessButFoundDifferentToken, stream.CurrentToken);
                    }

                    // We're not consuming the dot here, we'll wait until down below handles path dereferencing that needs it to be there.

                    variable = new ExpressionToken(variable.Value, ExpressionTokenCategory.NullSafeRangeVariableDereference, providesNullSafeAccess: true);
                }

                yield return variable;

                if (stream.CurrentToken == ExpressionToken.Semicolon)
                {
                    stream.ConsumeCurrent();

                    while (stream.CurrentToken == ExpressionToken.Whitespace)
                    {
                        stream.ConsumeCurrent();
                    }

                    // The only use case we have for a semicolon after a range variable is in reading
                    // off the current loop iteration index in a foreach loop, so the second variable is
                    // not allowed to be indexed/sliced. Don't check for a square bracket here to avoid that.

                    if (stream.CurrentToken != ExpressionToken.At)
                    {
                        throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedRangeVariableAfterSemicolonButFoundTokenInstead, stream.CurrentToken);
                    }

                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.RangeVariable, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace, ExpressionToken.Colon, ExpressionToken.Dot, ExpressionToken.ArrowBody);
                }

                if (stream.CurrentToken == ExpressionToken.Whitespace)
                {
                    while (stream.CurrentToken == ExpressionToken.Whitespace)
                    {
                        stream.ConsumeCurrent();
                    }

                    if (stream.CurrentToken == ExpressionToken.LetterI)
                    {
                        var token = TokenUntilMatchedWith(stream, ExpressionTokenCategory.In, ExpressionToken.Whitespace);

                        if (token.Value != ExpressionToken.In)
                        {
                            throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedInKeywordButFoundUnexpectedToken, token.Value);
                        }

                        yield return token;
                    }
                }
                else
                {
                    while (stream.CurrentToken == ExpressionToken.Dot || stream.CurrentToken == ExpressionToken.QuestionMark)
                    {
                        var isNullSafeReference = stream.CurrentToken == ExpressionToken.QuestionMark;

                        if (isNullSafeReference)
                        {
                            stream.ConsumeCurrent();

                            if (stream.CurrentToken != ExpressionToken.Dot)
                            {
                                throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedDotAfterQuestionMarkForNullSafePropertyAccessButFoundDifferentToken, stream.CurrentToken);
                            }

                            stream.ConsumeCurrent();

                            yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.NullSafePropertyDereference, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace, ExpressionToken.Colon, ExpressionToken.Dot, ExpressionToken.QuestionMark);
                        }
                        else
                        {
                            stream.ConsumeCurrent();

                            if (stream.CurrentToken == ExpressionToken.Dot)
                            {
                                stream.ConsumeCurrent();

                                yield return new ExpressionToken("..", ExpressionTokenCategory.RangeExpressionOperator);
                            }
                            else
                            {
                                var token = TokenUntilMatchedWith(stream, ExpressionTokenCategory.PropertyDereference, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.CloseSquareBracket, ExpressionToken.Whitespace, ExpressionToken.Colon, ExpressionToken.Dot, ExpressionToken.QuestionMark);

                                if (stream.CurrentToken == ExpressionToken.QuestionMark)
                                {
                                    yield return new ExpressionToken(token.Value, ExpressionTokenCategory.NullSafePropertyDereference);
                                }
                                else
                                {
                                    yield return token;
                                }
                            }
                        }
                    }
                }
            }
            else if (stream.CurrentToken == ExpressionToken.LetterA)
            {
                var token = TokenUntilMatchedWith(stream, ExpressionTokenCategory.As, ExpressionToken.Whitespace);

                if (token.Value != ExpressionToken.As)
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedAsKeywordButFoundUnexpectedToken, token.Value);
                }

                yield return token;
            }
            else if (stream.CurrentToken == ExpressionToken.LetterI)
            {
                var token = TokenUntilMatchedWith(stream, ExpressionTokenCategory.Into, ExpressionToken.Whitespace);

                if (token.Value != ExpressionToken.Into)
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedIntoKeywordButFoundUnexpectedToken, token.Value);
                }

                if (mode == EvaluationMode.PropertyValue)
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedNamedPropertyOrRangeVariableButFoundUnexpectedToken, stream.CurrentToken, stream.Position);
                }

                while (stream.CurrentToken == ExpressionToken.Whitespace)
                {
                    stream.ConsumeCurrent();
                }

                if (stream.CurrentToken == ExpressionToken.SingleQuote)
                {
                    stream.ConsumeCurrent();

                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.GeneratedNameIdentifier, ExpressionToken.SingleQuote);
                }
                else if (stream.CurrentToken == ExpressionToken.At)
                {
                    yield return TokenUntilMatchedWith(stream, ExpressionTokenCategory.RangeVariable, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace, ExpressionToken.ArrowBody);
                }
                else
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedStringLiteralPropertyNameButFoundDifferentToken, stream.CurrentToken);
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
            else if (stream.CurrentToken == ExpressionToken.QuestionMark)
            {
                stream.ConsumeCurrent();

                if (stream.CurrentToken == ExpressionToken.QuestionMark)
                {
                    stream.ConsumeCurrent();
                    yield return TokenFromCurrent(stream, ExpressionTokenCategory.NullCoalescing);
                }
                else
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedNullCoalescingOperatorButFoundSingleQuestionMark);
                }
            }
            else if (stream.CurrentToken == ExpressionToken.RangeEndIndexer)
            {
                yield return TokenFromCurrent(stream, ExpressionTokenCategory.RangeEndIndexer);
            }
            else if (char.IsNumber(stream.CurrentToken) || stream.CurrentToken == ExpressionToken.DecimalPoint || stream.CurrentToken == ExpressionToken.Caret)
            {
                if (char.IsNumber(stream.CurrentToken))
                {
                    var number = TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.NumericLiteral, char.IsNumber);

                    if (stream.TryMatchNextAndConsume(x => x == ExpressionToken.Dot))
                    {
                        if (stream.TryMatchNextAndConsume(x => x == ExpressionToken.Dot))
                        {
                            yield return number;

                            yield return TokenFrom("..", ExpressionTokenCategory.RangeExpressionOperator);
                        }
                        else
                        {
                            var precision = TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.NumericLiteral, char.IsNumber);

                            yield return TokenFrom($"{number.Value}.{precision.Value}", ExpressionTokenCategory.NumericLiteral);
                        }
                    }
                    else
                    {
                        yield return number;
                    }
                }
                else if (stream.TryMatchNextAndConsume(x => x == ExpressionToken.Dot))
                {
                    if (stream.TryMatchNextAndConsume(x => x == ExpressionToken.Dot))
                    {
                        yield return TokenFrom("..", ExpressionTokenCategory.RangeExpressionOperator);
                    }
                    else if (stream.TryMatchNextAndConsume(char.IsNumber))
                    {
                        var precision = TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.NumericLiteral, char.IsNumber);
                        yield return TokenFrom($"0.{precision.Value}", ExpressionTokenCategory.NumericLiteral);
                    }
                    else
                    {
                        throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedDotForRangeOperatorOrNumericPrecisionButFoundDifferentToken, stream.CurrentToken);
                    }
                }
                else if (stream.TryMatchNextAndConsume(x => x == ExpressionToken.Caret))
                {
                    yield return TokenFrom(ExpressionToken.Caret.ToString(), ExpressionTokenCategory.IndexFromEndOperator);

                    foreach(var token in ReadTokenFrom(stream, mode))
                    {
                        yield return token;
                    }
                }
                else
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.UnrecognizedNumericLiteralFormat, stream.CurrentToken);
                }
            }
            else if (char.IsLetter(stream.CurrentToken))
            {
                var possibleBoolToken = TokenUntilMatchedWith(stream, ExpressionTokenCategory.BooleanLiteral, ExpressionToken.Comma, ExpressionToken.CloseParentheses, ExpressionToken.Whitespace);

                if (bool.TryParse(possibleBoolToken.Value, out var value))
                {
                    yield return possibleBoolToken;
                }
                else if (possibleBoolToken.Value == "null")
                {
                    yield return TokenFrom(ExpressionToken.NullLiteral, ExpressionTokenCategory.NullLiteral);
                }
                else
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.ExpectedBooleanLiteralTokenButFoundUnknownToken, possibleBoolToken.Value);
                }
            }
            else if (stream.CurrentToken == ExpressionToken.Whitespace)
            {
                stream.ConsumeCurrent();
            }
            else
            {
                throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.UnableToCategorizeTokenAtPosition, stream.CurrentToken, stream.Position);
            }
        }
    }    
}
