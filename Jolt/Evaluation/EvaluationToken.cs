using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class EvaluationToken
    {
        public static EvaluationToken From(IJsonToken transformer) => new EvaluationToken(default, default, default, transformer);

        public string? PropertyName { get; }
        public string? ResolvedPropertyName { get; set; }
        public IJsonToken? ParentToken { get; }
        public IJsonToken CurrentTransformerToken { get; }
        public SourceToken? CurrentSource { get; }
        public bool IsPendingValueEvaluation { get; }
        public RangeVariable? ParentRangeVariable { get; }
        public bool IsWithinStatementBlock { get; }
        public bool IsWithinMatchBlock { get; }

        public EvaluationToken(string? propertyName, string? resolvedPropertyName, IJsonToken? parentToken, IJsonToken currentTransformerToken, SourceToken? currentSource = null, bool isPendingValueEvaluation = false, RangeVariable? parentRangeVariable = null, bool isWithinStatementBlock = false, bool isWithinMatchBlock = false)
        {
            PropertyName = propertyName;
            ResolvedPropertyName = resolvedPropertyName;
            ParentToken = parentToken;
            CurrentTransformerToken = currentTransformerToken;
            CurrentSource = currentSource;
            IsPendingValueEvaluation = isPendingValueEvaluation;
            ParentRangeVariable = parentRangeVariable;
            IsWithinStatementBlock = isWithinStatementBlock;
            IsWithinMatchBlock = isWithinMatchBlock;
        }

        public EvaluationToken(EvaluationToken token, IJsonToken currentTransformerToken, SourceToken? currentSource = null)
            : this(token.PropertyName, token.ResolvedPropertyName, token.ParentToken, currentTransformerToken, currentSource, token.IsPendingValueEvaluation, token.ParentRangeVariable, token.IsWithinStatementBlock, token.IsWithinMatchBlock)
        {
        }
    }
}
