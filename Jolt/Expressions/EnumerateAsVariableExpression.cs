using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class EnumerateAsVariableExpression : Expression
    {
        public RangeVariableExpression Variable { get; }
        public Expression EnumerationSource { get; }

        public EnumerateAsVariableExpression(RangeVariableExpression variable, Expression enumerationSource)
        {
            Variable = variable;
            EnumerationSource = enumerationSource;
        }
    }
}
