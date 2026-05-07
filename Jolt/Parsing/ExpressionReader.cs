using Jolt.Exceptions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    internal sealed class ExpressionReader : TokenStream<ExpressionToken>
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

            ConsumeCurrent();

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
}
