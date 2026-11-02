using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class RangeExpression : Expression
    {
        public RangeIndexExpression StartIndex { get; }
        public RangeIndexExpression EndIndex { get; }

        /// <summary>
        /// Gets whether this is a single index (e.g. "[1]") rather than a range (e.g. "[1..2]"), in which case the
        /// start and end are the same index.
        /// </summary>
        public bool IsSingleIndex => ReferenceEquals(StartIndex, EndIndex);

        public RangeExpression(RangeIndexExpression startIndex, RangeIndexExpression endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
