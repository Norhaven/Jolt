using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class DereferencedPath
    {
        public RangeVariable SourceVariable { get; }
        public IJsonToken ObtainableToken { get; }
        public string[] MissingPaths { get; }

        public DereferencedPath(RangeVariable sourceVariable, IJsonToken obtainableToken, string[]? missingPaths = default)
        {
            SourceVariable = sourceVariable;
            ObtainableToken = obtainableToken;
            MissingPaths = missingPaths ?? Array.Empty<string>();
        }
    }
}
