using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal interface IAtomExpressionParser : ISpecializedExpressionParser
    {
        T GetExpressionParserOf<T>() where T : ISpecializedExpressionParser;
    }
}
