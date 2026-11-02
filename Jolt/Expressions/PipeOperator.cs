using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    /// <summary>
    /// How a method call's first parameter was supplied.
    /// </summary>
    public enum PipeOperator
    {
        /// <summary>
        /// The method was called directly (e.g. "#trim(@x)").
        /// </summary>
        None = 0,

        /// <summary>
        /// The first parameter was piped into the method (e.g. "@x->#trim()").
        /// </summary>
        Pipe,

        /// <summary>
        /// The first parameter was piped into the method only if it isn't null or missing (e.g. "@x?->#trim()"),
        /// otherwise the method and the rest of its chain are skipped and the chain gives null.
        /// </summary>
        NullConditionalPipe
    }
}
