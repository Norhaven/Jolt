using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class RangeVariable
    {
        public string Name { get; }
        public IJsonToken? Value { get; set; }

        public RangeVariable(string name)
        {
            Name = name;
            Value = default;
        }

        public RangeVariable(string name, IJsonToken value)
        {
            Name = name;
            Value = value;
        }
    }
}
