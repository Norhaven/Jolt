using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal abstract class SpecializedExpressionParser : ISpecializedExpressionParser
    {
        protected readonly ExpressionReader _reader;
        protected readonly IAtomExpressionParser _atomParser;

        public abstract AtomType AtomType { get; }

        public SpecializedExpressionParser(ExpressionReader reader, IAtomExpressionParser atomParser)
        {
            _reader = reader;
            _atomParser = atomParser;
        }

        public abstract bool CanParse(IJsonContext context);

        public virtual Expression? Parse(IJsonContext context)
        {
            if (!TryParse(context, out var expression))
            {
                return default;
            }

            return expression;
        }

        public abstract bool TryParse(IJsonContext context, out Expression? expression);

        protected T GetExpressionParserOf<T>() where T : ISpecializedExpressionParser
        {
            return _atomParser.GetExpressionParserOf<T>();
        }

        /// <summary>
        /// Parses any methods piped from an expression that has just been read (e.g. "[1, 2]->#joinWith(',')"), which
        /// gives the expression as the first parameter of the first piped method. Otherwise, the expression is returned as it is.
        /// </summary>
        protected Expression ParsePipedMethodCallsFrom(IJsonContext context, Expression expression)
        {
            // The reader is checked for completion first, since the expression may be the whole property value.

            if (_reader.IsCompleted || !MethodCallExpressionParser.IsStartOfPipe(_reader.CurrentToken))
            {
                return expression;
            }

            if (!GetExpressionParserOf<MethodCallExpressionParser>().TryParse(context, out var pipedMethod, expression))
            {
                throw context.CreateParsingErrorFor<ExpressionParser>(ExceptionCode.UnableToCompleteParsingOfPipedMethodCall);
            }

            return pipedMethod!;
        }

        protected bool IsInCategory(params ExpressionTokenCategory[] categories)
        {
            return categories?.Any(_reader.IsCategory) == true;
        }
    }
}
