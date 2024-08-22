using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class VariableAlias
    {
        public object Source { get; }
        public RangeVariable Variable { get; }

        public VariableAlias(object source, RangeVariable variable)
        {
            Source = source;
            Variable = variable;
        }
    }
}
