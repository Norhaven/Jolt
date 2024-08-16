using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class LambdaMethod
    {
        public RangeVariable Variable { get; }
        public Expression Body { get; }

        public LambdaMethod(RangeVariable variable, Expression body)
        {
            Variable = variable;
            Body = body;
        }
    }
}
