using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class RangeExpression : Expression
    {
        public Index StartIndex { get; }
        public Index EndIndex { get; }

        public RangeExpression(Index startIndex, Index endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
