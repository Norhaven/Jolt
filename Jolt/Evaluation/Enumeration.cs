using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class Enumeration
    {
        public RangeVariable Variable { get; }
        public RangeVariable? IndexVariable { get; }
        public object? Source { get; }

        public Enumeration(RangeVariable variable, object? source, RangeVariable? indexVariable = null)
        {
            Variable = variable; 
            Source = source;
            IndexVariable = indexVariable;
        }
    }
}
