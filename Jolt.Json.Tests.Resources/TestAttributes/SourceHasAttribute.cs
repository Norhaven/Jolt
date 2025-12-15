using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    /// <summary>
    /// Sets a JSON property value in the source document by name and type. 
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <param name="name">The property name that contains the value.</param>
    /// <param name="value">The value to set.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class SourceHasAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the property that contains the value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        public SourceValueType Type { get; }

        /// <summary>
        /// Gets the value that should be set.
        /// </summary>
        public object Value { get; }

        public SourceHasAttribute(SourceValueType type, string name, object value)
        {
            Type = type;
            Name = name;
            Value = value;
        }
    }
}
