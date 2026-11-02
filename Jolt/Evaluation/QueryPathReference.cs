using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    /// <summary>
    /// A JSON path written in an expression (e.g. "$.some.path"), as opposed to a string literal that happens
    /// to look like one (e.g. "'$.some.path'"), which is only ever treated as text.
    /// </summary>
    internal sealed class QueryPathReference
    {
        public string Path { get; }

        public QueryPathReference(string path)
        {
            Path = path;
        }

        public override string ToString() => Path;
    }
}
