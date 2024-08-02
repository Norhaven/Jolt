using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a primitive JSON value. This will not be an object or an array.
    /// </summary>
    public interface IJsonValue : IJsonToken
    {
        /// <summary>
        /// Gets the JSON type of the value that this represents.
        /// </summary>
        JsonValueType ValueType { get; }

        /// <summary>
        /// Determines whether the underlying JSON value is of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to check against.</typeparam>
        /// <returns>True if the object is of the specified type, false otherwise.</returns>
        bool IsObject<T>();
    }
}
