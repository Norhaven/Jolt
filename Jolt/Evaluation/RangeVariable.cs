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
        public bool ProvidesNullSafeAccess { get; }

        public RangeVariable(string name, bool providesNullSafeAccess = false)
        {
            Name = name;
            Value = default;
            ProvidesNullSafeAccess = providesNullSafeAccess;
        }

        public RangeVariable(string name, IJsonToken value)
        {
            Name = name;
            Value = value;
        }
    }
}
