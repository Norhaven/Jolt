using Jolt.Evaluation;
using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing.Readers
{
    public abstract class BasicTokenReader
    {
        protected readonly IMessageProvider _messageProvider;

        public BasicTokenReader(IMessageProvider messageProvider)
        {
            _messageProvider = messageProvider;
        }

        public abstract IEnumerable<ExpressionToken> ReadTokenFrom(ITokenStream<char> stream, EvaluationMode mode);

        protected ExpressionToken TokenFrom(string value, ExpressionTokenCategory category)
        {
            return new ExpressionToken(value, category);
        }

        protected ExpressionToken TokenFromCurrent(ITokenStream<char> stream, ExpressionTokenCategory category)
        {
            return new ExpressionToken(stream.ConsumeCurrent().ToString(), category);
        }

        protected ExpressionToken TokenUntilNotMatchedWith(ITokenStream<char> stream, ExpressionTokenCategory category, Func<char, bool> isMatch)
        {
            if (!stream.TryConsumeUntil(x => !isMatch(x), out var consumedTokens))
            {
                if (!stream.IsCompleted)
                {
                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.UnableToLocateExpectedCharactersInExpression);
                }
            }

            return new ExpressionToken(new string(consumedTokens), category);
        }

        protected ExpressionToken TokenUntilMatchedWith(ITokenStream<char> stream, ExpressionTokenCategory category, params char[] tokens)
        {
            if (!stream.TryConsumeUntil(x => tokens.Contains(x), out var consumedTokens))
            {
                if (!stream.IsCompleted)
                {
                    var expectedCharacters = string.Join(", ", tokens);

                    throw _messageProvider.CreateErrorFor<TokenReader>(MessageCategory.Parsing, ExceptionCode.UnableToLocateSpecificExpectedCharactersInExpression, expectedCharacters);
                }
            }

            return new ExpressionToken(new string(consumedTokens), category);
        }
    }
}
