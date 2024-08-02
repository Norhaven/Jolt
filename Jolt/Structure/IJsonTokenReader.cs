using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a way of reading JSON structures from different sources.
    /// </summary>
    public interface IJsonTokenReader
    {
        /// <summary>
        /// Reads a JSON string into a JSON structure.
        /// </summary>
        /// <param name="json">The JSON string to read.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the JSON could be read, null otherwise.</returns>
        IJsonToken? Read(string json);

        /// <summary>
        /// Reads a series of JSON structures into a JSON array.
        /// </summary>
        /// <param name="tokens">The JSON structures to include in the array.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the JSON could be read, null otherwise.</returns>
        IJsonToken? CreateArrayFrom(IEnumerable<IJsonToken> tokens);

        /// <summary>
        /// Reads a JSON structure from a .Net instance.
        /// </summary>
        /// <param name="value">The instance to read.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the JSON could be read, null otherwise.</returns>
        IJsonToken? CreateTokenFrom(object? value);

        /// <summary>
        /// Reads a series of JSON structures into a JSON object. 
        /// </summary>
        /// <param name="tokens">The JSON structures to include in the object.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the JSON could be read, null otherwise.</returns>
        IJsonToken? CreateObjectFrom(IEnumerable<IJsonToken>? tokens);
    }
}
