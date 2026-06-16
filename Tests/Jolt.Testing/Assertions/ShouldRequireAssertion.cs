using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Assertions
{
    public sealed class ShouldRequireAssertion<T>
    {
        public T ActualValue { get; }

        public ShouldRequireAssertion(T actualValue)
        {
            ActualValue = actualValue;
        }
    }
}
