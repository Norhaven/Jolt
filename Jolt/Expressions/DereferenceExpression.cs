using Jolt.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class DereferenceExpression : Expression
    {
        public string PropertyName { get; }
        public bool IsNullSafe { get; }

        /// <summary>
        /// Gets the indexes or slices applied in turn to the property's value (e.g. the "[0]" in "@x.items[0].name", or the "[1]" and "[0]" in "@x.rows[1][0]").
        /// </summary>
        public RangeExpression[] Indexers { get; }

        public DereferenceExpression(string propertyName, bool isNullSafe, RangeExpression[]? indexers = default)
        {
            PropertyName = propertyName;
            IsNullSafe = isNullSafe;
            Indexers = indexers ?? Array.Empty<RangeExpression>();
        }
    }
}
