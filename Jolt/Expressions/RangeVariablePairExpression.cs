using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class RangeVariablePairExpression : RangeVariableExpression
    {
        public RangeVariableExpression SecondVariable { get; }

        public RangeVariablePairExpression(RangeVariableExpression firstVariable, RangeVariableExpression secondVariable)
            : base(firstVariable.Name)
        {
            SecondVariable = secondVariable;
        }
    }
}
