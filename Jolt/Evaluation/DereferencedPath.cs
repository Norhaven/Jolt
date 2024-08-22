using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class DereferencedPath
    {
        public IJsonToken ObtainableToken { get; }
        public string[] MissingPaths { get; }

        public DereferencedPath(IJsonToken obtainableToken, string[]? missingPaths = default)
        {
            ObtainableToken = obtainableToken;
            MissingPaths = missingPaths ?? Array.Empty<string>();
        }
    }
}
