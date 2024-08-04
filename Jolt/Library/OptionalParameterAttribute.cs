using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    internal sealed class OptionalParameterAttribute : Attribute
    {
        public object DefaultValue { get; }

        public OptionalParameterAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }
    }
}
