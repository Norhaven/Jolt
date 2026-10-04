using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Parsing.Parsers;
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
        public bool TryParseExpression(IEnumerable<ExpressionToken> tokens, IJsonContext context, out Expression? expression) => TryParseExpression(new ExpressionReader(tokens, context), context, out expression);

        private bool TryParseExpression(ExpressionReader reader, IJsonContext context, out Expression? expression)
        {
            expression = default;

            var atomParser = new AtomExpressionParser(reader);

            var availableParsers = new ISpecializedExpressionParser[]
            {
                new LogicalNotExpressionParser(reader, atomParser),
                new ParenthesizedExpressionParser(reader, atomParser),
                new MethodCallExpressionParser(reader, atomParser),
                new JsonPathExpressionParser(reader, atomParser),
                new RangeExpressionParser(reader, atomParser),
                new RangeVariableExpressionParser(reader, atomParser),
                new ArrayLiteralExpressionParser(reader, atomParser),
                new ObjectLiteralExpressionParser(reader, atomParser),
                new LiteralExpressionParser(reader, atomParser),
                new TypeLiteralExpressionParser(reader, atomParser),
                new DiscardExpressionParser(reader, atomParser)
            };

            atomParser.AvailableParsers = availableParsers;

            if (!atomParser.CanParse(context))
            {
                return false;
            }

            expression = atomParser.Parse(context);

            // An expression must be complete on its own, so anything after it is a mistake (e.g. "#valueOf($.a) @b" or
            // "#valueOf($.a))") rather than something to ignore.

            if (!reader.IsCompleted)
            {
                var unexpected = reader.CurrentToken;
                var remaining = reader.ConsumeUntilEnd();

                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnexpectedContentAfterExpression, unexpected.Value)
                    .WithSpan(ExpressionSpan.Cover(remaining[0].Span, remaining[remaining.Length - 1].Span));
            }

            return true;
        }
    }
}
