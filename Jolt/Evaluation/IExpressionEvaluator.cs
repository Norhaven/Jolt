using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public interface IExpressionEvaluator
    {
        EvaluationResult Evaluate(EvaluationContext context);
    }
}
