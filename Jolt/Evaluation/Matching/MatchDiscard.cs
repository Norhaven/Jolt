using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation.Matching
{
    internal sealed class MatchDiscard
    {
        public MatchKind Kind { get; }
        public string? VariableName { get; }

        public MatchDiscard(MatchKind kind, string? variableName = default)
        {
            Kind = kind;
            VariableName = variableName;
        }
    }
}
