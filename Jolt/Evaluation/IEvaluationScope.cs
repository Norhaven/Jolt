using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public interface IEvaluationScope
    {
        int VariableCount { get; }
        IEnumerable<IJsonToken> AvailableClosures { get; }
        IEnumerable<RangeVariable> AvailableVariables { get; }

        bool ContainsVariable(string variableName);
        bool TryGetVariable(string variableName, out RangeVariable? variable);
        IEvaluationScope CreateClosureOver(IJsonToken token);
        IEvaluationScope AddOrUpdateVariable(RangeVariable variable, bool forceApplyToCurrent = false);

        IEvaluationScope RemoveCurrentClosure();
        IEvaluationScope RemoveCurrentVariables();
    }
}
