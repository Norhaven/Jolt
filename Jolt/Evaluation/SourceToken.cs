using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class SourceToken
    {
        public int Index { get; }
        public IJsonToken? Property { get; }

        public SourceToken(int index, IJsonToken? property)
        {
            Index = index;
            Property = property;
        }
    }
}
