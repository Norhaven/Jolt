using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class LambdaMethodExpression : Expression
    {
        public RangeVariableExpression Variable { get; }
        public Expression Body { get; }

        public LambdaMethodExpression(RangeVariableExpression variable, Expression body)
        {
            Variable = variable;
            Body = body;
        }
    }
}
