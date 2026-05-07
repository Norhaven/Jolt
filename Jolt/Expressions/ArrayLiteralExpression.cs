using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class ArrayLiteralExpression : Expression
    {
        public Expression[] Elements { get; }

        public ArrayLiteralExpression(IEnumerable<Expression> elements)
        {
            Elements = elements.ToArray();
        }
    }
}
