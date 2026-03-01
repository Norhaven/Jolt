using Jolt.Evaluation;
using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing.Readers
{
    internal sealed class NumericOrRangeExpressionTokenReader : BasicTokenReader
    {
        private sealed class InferrableToken
        {
            private readonly string _tokenValue;

            public bool ContainsAtLeastOneCaret => _tokenValue.Contains(ExpressionToken.Caret.ToString());
            public bool ContainsExactlyOneCaret => _tokenValue.Count(x => x == ExpressionToken.Caret) == 1;
            public bool ContainsAtLeastOneDot => _tokenValue.Contains(ExpressionToken.Dot.ToString());
            public bool ContainsTwoDots => _tokenValue.Contains($"{ExpressionToken.Dot}{ExpressionToken.Dot}");
            public bool ContainsOneDot => _tokenValue.Contains(ExpressionToken.Dot.ToString()) && !ContainsTwoDots;
            public bool ContainsExactlyOneDot => _tokenValue.Count(x => x == ExpressionToken.Dot) == 1;
            public bool ContainsAtLeastOneNumericCharacter => _tokenValue.Any(char.IsNumber);
            public bool ContainsOnlyNumericCharacters => !ContainsAtLeastOneCaret && !ContainsOneDot && !ContainsTwoDots && ContainsAtLeastOneNumericCharacter;            
            public int IndexOfCaret => _tokenValue.IndexOf(ExpressionToken.Caret);
            public bool StartsWithCaret => _tokenValue.StartsWith(ExpressionToken.Caret.ToString());
            public bool StartsWithDot => _tokenValue.StartsWith(ExpressionToken.Dot.ToString());
            public bool EndsWithDot => _tokenValue.EndsWith(ExpressionToken.Dot.ToString());
            public bool IsOnlyEndIndexRange => ContainsExactlyOneCaret && StartsWithCaret && ContainsAtLeastOneNumericCharacter && !ContainsAtLeastOneDot;
            public bool IsOnlyNumeric => !ContainsAtLeastOneCaret && !ContainsAtLeastOneDot && ContainsAtLeastOneNumericCharacter;
            public bool IsOnlyNumericWithDecimal => !ContainsAtLeastOneCaret && ContainsExactlyOneDot && ContainsAtLeastOneNumericCharacter;
            public bool IsOnlyNumericWithCaret => ContainsExactlyOneCaret && StartsWithCaret && ContainsAtLeastOneNumericCharacter && !ContainsAtLeastOneDot;

            public InferrableToken(string tokenValue)
            {
                _tokenValue = tokenValue;
            }
        }

        private sealed class TokenInference
        {
            private readonly NumericOrRangeExpressionTokenReader _reader;
            private readonly TokenStream<char> _stream;

            public TokenInference(NumericOrRangeExpressionTokenReader reader, TokenStream<char> stream)
            {
                _reader = reader;
                _stream = stream;
            }

            public ExpressionToken InferToken()
            {
                var token = _reader.TokenUntilNotMatchedWith(_stream, ExpressionTokenCategory.RangeVariable, IsEndIndexOrDotOrNumeric);

                var inferrableToken = new InferrableToken(token.Value);

                if (inferrableToken.IsOnlyNumeric || (inferrableToken.IsOnlyNumericWithDecimal && !inferrableToken.EndsWithDot))
                {
                    return new ExpressionToken(token.Value, ExpressionTokenCategory.NumericLiteral);
                }

                if (inferrableToken.IsOnlyEndIndexRange)
                {
                    return new ExpressionToken(token.Value, ExpressionTokenCategory.RangeExpression);
                }

                if (inferrableToken.ContainsTwoDots)
                {
                    if (inferrableToken.EndsWithDot || inferrableToken.StartsWithDot)
                    {
                        return new ExpressionToken(token.Value, ExpressionTokenCategory.RangeExpression);
                    }
                    
                    var parts = token.Value.Split(new[] { $"{ExpressionToken.Dot}{ExpressionToken.Dot}" }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        var left = new InferrableToken(parts[0]);
                        var right = new InferrableToken(parts[1]);
                        var leftPartIsValid = left.IsOnlyNumeric || left.IsOnlyNumericWithDecimal || left.IsOnlyNumericWithCaret;
                        var rightPartIsValid = right.IsOnlyNumeric || right.IsOnlyNumericWithDecimal || right.IsOnlyNumericWithCaret;

                        if (leftPartIsValid && rightPartIsValid)
                        {
                            return new ExpressionToken(token.Value, ExpressionTokenCategory.RangeExpression);
                        }
                    }
                }

                throw _reader._messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.InvalidRangeExpressionFormat, token.Value);
            }

            private bool IsEndIndexOrDotOrNumeric(char c) => IsEndIndex(c) || IsDot(c) || IsNumeric(c);
            private bool IsEndIndex(char c) => c == ExpressionToken.RangeEndIndexer;
            private bool IsDot(char c) => c == ExpressionToken.Dot;
            private bool IsNumeric(char c) => char.IsNumber(c);
        }

        public NumericOrRangeExpressionTokenReader(IMessageProvider messageProvider) 
            : base(messageProvider)
        {
        }

        public override IEnumerable<ExpressionToken> ReadTokenFrom(ITokenStream<char> stream, EvaluationMode mode)
        {
            var token = TokenUntilNotMatchedWith(stream, ExpressionTokenCategory.RangeVariable, IsEndIndexOrDotOrNumeric);

            var inferrableToken = new InferrableToken(token.Value);

            if (inferrableToken.IsOnlyNumeric || (inferrableToken.IsOnlyNumericWithDecimal && !inferrableToken.EndsWithDot))
            {
                yield return new ExpressionToken(token.Value, ExpressionTokenCategory.NumericLiteral);
            }
            else if (inferrableToken.IsOnlyEndIndexRange)
            {
                yield return new ExpressionToken(token.Value, ExpressionTokenCategory.RangeExpression);
            }
            else if (inferrableToken.ContainsTwoDots)
            {
                if (inferrableToken.EndsWithDot || inferrableToken.StartsWithDot)
                {
                    yield return new ExpressionToken(token.Value, ExpressionTokenCategory.RangeExpression);
                    yield break;
                }

                var parts = token.Value.Split(new[] { $"{ExpressionToken.Dot}{ExpressionToken.Dot}" }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    var left = new InferrableToken(parts[0]);
                    var right = new InferrableToken(parts[1]);
                    var leftPartIsValid = left.IsOnlyNumeric || left.IsOnlyNumericWithDecimal || left.IsOnlyNumericWithCaret;
                    var rightPartIsValid = right.IsOnlyNumeric || right.IsOnlyNumericWithDecimal || right.IsOnlyNumericWithCaret;

                    if (leftPartIsValid && rightPartIsValid)
                    {
                        yield return new ExpressionToken(token.Value, ExpressionTokenCategory.RangeExpression);
                        yield break;
                    }
                }

                throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.InvalidRangeExpressionFormat, token.Value);
            }
            else
            {
                throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.InvalidRangeExpressionFormat, token.Value);
            }
        }

        private bool IsEndIndexOrDotOrNumeric(char c) => IsEndIndex(c) || IsDot(c) || IsNumeric(c);
        private bool IsEndIndex(char c) => c == ExpressionToken.RangeEndIndexer;
        private bool IsDot(char c) => c == ExpressionToken.Dot;
        private bool IsNumeric(char c) => char.IsNumber(c);
    }
}
