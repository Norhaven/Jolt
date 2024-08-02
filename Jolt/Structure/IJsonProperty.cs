using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a JSON property.
    /// </summary>
    public interface IJsonProperty : IJsonToken
    {
        /// <summary>
        /// Gets the name of this property.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the value of this property if present, null otherwise.
        /// </summary>
        public IJsonToken? Value { get; }
    }
}
