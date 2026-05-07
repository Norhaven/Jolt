using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class RangeIndexExpression : Expression
    {
        public Expression Index { get; }
        public bool IsOffsetFromEnd { get; }

        public RangeIndexExpression(Expression index, bool isOffsetFromEnd)
        {
            Index = index;
            IsOffsetFromEnd = isOffsetFromEnd;
        }
    }
}
