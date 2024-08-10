using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public class RangeVariableExpression : Expression
    {
        public string Name { get; }

        public RangeVariableExpression(string name)
        {
            Name = name;
        }
    }
}
