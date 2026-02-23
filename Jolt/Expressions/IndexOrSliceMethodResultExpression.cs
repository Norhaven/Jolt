using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class IndexOrSliceMethodResultExpression : MethodCallExpression
    {
        public RangeExpression ResultRange { get; }

        public IndexOrSliceMethodResultExpression(RangeExpression resultRange, MethodCallExpression method)
            :base(method.Signature, method.ParameterValues, method.GeneratedName, method.GeneratedVariable)
        {
            ResultRange = resultRange;
        }
    }
}
