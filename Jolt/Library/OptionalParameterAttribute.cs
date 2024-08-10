using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a marker that indicates that the parameter is optional and 
    /// provides a default value for when it has been omitted.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class OptionalParameterAttribute : Attribute
    {
        public object DefaultValue { get; }

        public OptionalParameterAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }
    }
}
