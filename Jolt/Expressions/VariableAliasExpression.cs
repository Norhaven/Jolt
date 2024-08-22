using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class VariableAliasExpression : Expression
    {
        public PathExpression? SourcePath { get; }
        public RangeVariableExpression? SourceVariable { get; }
        public RangeVariableExpression AliasVariable { get; }

        public bool IsSourceFromPath => SourcePath != null;

        public VariableAliasExpression(RangeVariableExpression sourceVariable, RangeVariableExpression aliasVariable)
        {
            SourceVariable = sourceVariable;
            AliasVariable = aliasVariable;
        }

        public VariableAliasExpression(PathExpression sourcePath, RangeVariableExpression aliasVariable)
        {
            SourcePath = sourcePath;
            AliasVariable = aliasVariable;
        }
    }
}
