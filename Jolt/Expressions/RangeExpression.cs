using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class RangeExpression : Expression
    {
        public RangeIndexExpression StartIndex { get; }
        public RangeIndexExpression EndIndex { get; }

        public RangeExpression(RangeIndexExpression startIndex, RangeIndexExpression endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
