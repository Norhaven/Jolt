using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal interface ISpecializedExpressionParser
    {
        AtomType AtomType { get; }
        bool CanParse(IJsonContext context);
        Expression? Parse(IJsonContext context);
        bool TryParse(IJsonContext context, out Expression? expression);
    }
}
