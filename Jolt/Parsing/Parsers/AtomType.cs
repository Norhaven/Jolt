using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing.Parsers
{
    internal enum AtomType
    {
        Unknown = 0,
        Atom,
        ParenthesizedExpression,
        MethodCall,
        JsonPath,
        RangeExpression,
        RangeVariable,
        ArrayLiteral,
        Literal
    }
}
