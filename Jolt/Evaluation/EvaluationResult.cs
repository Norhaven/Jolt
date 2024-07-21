using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public class EvaluationResult
    {
        public string OriginalPropertyName { get; }
        public string NewPropertyName { get; }
        public IJsonToken TransformedToken { get; }

        public EvaluationResult(string originalPropertyName, string newPropertyName, IJsonToken transformedToken)
        {
            OriginalPropertyName = originalPropertyName;
            NewPropertyName = newPropertyName;
            TransformedToken = transformedToken;
        }
    }
}
