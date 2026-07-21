using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class LambdaMethod
    {
        public RangeVariable Variable { get; }
        public RangeVariable? SecondVariable { get; }
        public Expression Body { get; }

        public LambdaMethod(RangeVariable variable, Expression body)
        {
            Variable = variable;
            Body = body;
        }

        public LambdaMethod(RangeVariable variable, RangeVariable? secondVariable, Expression body)
        {
            Variable = variable;
            SecondVariable = secondVariable;
            Body = body;
        }
    }
}
