using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public class EvaluationToken
    {
        public string PropertyName { get; }
        public string ResolvedPropertyName { get; set; }
        public IJsonToken? ParentToken { get; }
        public IJsonToken CurrentTransformerToken { get; }
        public int Index { get; }

        public EvaluationToken(string propertyName, string resolvedPropertyName, IJsonToken? parentToken, IJsonToken currentTransformerToken, int index = 0)
        {
            PropertyName = propertyName;
            ResolvedPropertyName = resolvedPropertyName;
            ParentToken = parentToken;
            CurrentTransformerToken = currentTransformerToken;
            Index = index;
        }
    }
}
