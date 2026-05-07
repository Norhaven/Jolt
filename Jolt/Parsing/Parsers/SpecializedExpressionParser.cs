using Jolt.Expressions;
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

        protected bool IsInCategory(params ExpressionTokenCategory[] categories)
        {
            return categories?.Any(_reader.IsCategory) == true;
        }
    }
}
