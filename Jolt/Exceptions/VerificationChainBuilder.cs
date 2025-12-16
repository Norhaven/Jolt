using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    internal sealed class VerificationChainBuilder<T> 
        where T :class?
    {
        public T Value { get; }

        public VerificationChainBuilder(T value) {
            Value = value;
        }
    }
}
