using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class LambdaMethod
    {
        public RangeVariable[] Variables { get; }
        public Expression Body { get; }

        public LambdaMethod(RangeVariable variable, Expression body)
        {
            Variables = new[] { variable };
            Body = body;
        }

        public LambdaMethod(RangeVariable variable, RangeVariable? secondVariable, Expression body)
        {
            Variables = new[] { variable, secondVariable };
            Body = body;
        }
    }
}
