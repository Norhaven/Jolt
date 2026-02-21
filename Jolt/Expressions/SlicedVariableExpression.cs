using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class SlicedVariableExpression : Expression
    {
        public RangeVariableExpression Variable { get; }
        public RangeExpression Range { get; }

        public SlicedVariableExpression(RangeVariableExpression variable, RangeExpression range)
        {
            Variable = variable;
            Range = range;
        }
    }
}
