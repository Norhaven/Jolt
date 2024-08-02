using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents an external method that may be registered for use within the Jolt ecosystem.
    /// </summary>
    public sealed class JoltExternalMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the external method when used from within a Jolt transformer, 
        /// if it should be different from the literal method name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Initializes an instance of <see cref="JoltExternalMethodAttribute"/> with the provided parameter.
        /// </summary>
        /// <param name="name">The name of the method when used within a Jolt transformer, if different from the method name.</param>
        public JoltExternalMethodAttribute(string? name = default)
        {
            Name = name;
        }
    }
}
