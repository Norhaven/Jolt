using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class LogicalNotExpression : Expression
    {
        public Expression Operand { get; }

        public LogicalNotExpression(Expression operand)
        {
            Operand = operand;
        }
    }
}
