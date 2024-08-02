using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a Jolt library method. This is reserved for use within Jolt.
    /// </summary>
    internal sealed class JoltLibraryMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the library method when used from within a Jolt transformer.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether the method is a value generator (i.e. it will generate its own property value due to evaluation).
        /// </summary>
        public bool IsValueGenerator { get; }

        /// <summary>
        /// Initializes an instance of <see cref="JoltLibraryMethodAttribute"/> with the provided parameters.
        /// </summary>
        /// <param name="name">The name of the library method.</param>
        /// <param name="isValueGenerator">Whether the method is a value generator.</param>
        public JoltLibraryMethodAttribute(string name, bool isValueGenerator = false)
        {
            Name = name;
            IsValueGenerator = isValueGenerator;
        }
    }
}
