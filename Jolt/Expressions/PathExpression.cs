using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public class PathExpression : Expression
    {
        public string PathQuery { get; }

        public PathExpression(string pathQuery)
        {
            PathQuery = pathQuery;
        }
    }
}
