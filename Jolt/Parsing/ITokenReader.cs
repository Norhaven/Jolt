using Jolt.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public interface ITokenReader
    {
        bool StartsWithMethodCall(string expression);
        IEnumerable<ExpressionToken> ReadToEnd(string expression, EvaluationMode mode);
    }
}
