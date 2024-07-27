using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class EvaluationResult
    {
        public string OriginalPropertyName { get; }
        public string NewPropertyName { get; }
        public IJsonToken TransformedToken { get; }
        public bool IsValuePendingEvaluation { get; }

        public EvaluationResult(string originalPropertyName, string newPropertyName, IJsonToken transformedToken, bool isValuePendingEvaluation = false)
        {
            OriginalPropertyName = originalPropertyName;
            NewPropertyName = newPropertyName;
            TransformedToken = transformedToken;
            IsValuePendingEvaluation = isValuePendingEvaluation;
        }
    }
}
