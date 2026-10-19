using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class DereferencedPath
    {
        public RangeVariable SourceVariable { get; }
        public IJsonToken? ObtainableToken { get; }
        public string[] MissingPaths { get; }

        /// <summary>
        /// Gets whether the path stopped early at a null-safe ('?.') dereference, meaning that the
        /// missing portion of the path should be treated as null rather than as an error.
        /// </summary>
        public bool IsNullSafe { get; }

        public DereferencedPath(RangeVariable sourceVariable, IJsonToken? obtainableToken, string[]? missingPaths = default, bool isNullSafe = false)
        {
            SourceVariable = sourceVariable;
            ObtainableToken = obtainableToken;
            MissingPaths = missingPaths ?? Array.Empty<string>();
            IsNullSafe = isNullSafe;
        }
    }
}
