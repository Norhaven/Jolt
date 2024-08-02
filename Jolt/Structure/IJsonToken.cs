using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a general JSON structure. This could be an object, array, property, or value.
    /// </summary>
    public interface IJsonToken
    {
        /// <summary>
        /// Gets the immediate parent JSON structure of this one, returning null if none was found.
        /// </summary>
        IJsonToken? Parent { get; }

        /// <summary>
        /// Gets the JSON type of this structure.
        /// </summary>
        JsonTokenType Type { get; }

        /// <summary>
        /// Gets the current structure cast as a JSON object.
        /// </summary>
        /// <returns>An instance of <see cref="IJsonObject"/>.</returns>
        IJsonObject AsObject();

        /// <summary>
        /// Gets the current structure cast as a JSON array.
        /// </summary>
        /// <returns>An instance of <see cref="IJsonArray"/>.</returns>
        IJsonArray AsArray();

        /// <summary>
        /// Gets the current structure cast as a JSON value.
        /// </summary>
        /// <returns>An instance of <see cref="IJsonValue"/>.</returns>
        IJsonValue AsValue();

        /// <summary>
        /// Selects a JSON structure using this one as the root of the query.
        /// </summary>
        /// <param name="path">The JSON query path.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the query returned a valid structure, null otherwise.</returns>
        IJsonToken? SelectTokenAtPath(string path);

        /// <summary>
        /// Creates a copy of this JSON structure.
        /// </summary>
        /// <returns>An instance of <see cref="IJsonToken"/> if the copy was created, null otherwise.</returns>
        IJsonToken? Copy();

        /// <summary>
        /// Clears the contents of this JSON structure. This will remove properties if this is an object, remove array elements from an array,
        /// or set the value to null depending on the type this is.
        /// </summary>
        void Clear();

        /// <summary>
        /// Converts the underlying structure to a .Net type of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The .Net type to convert to.</typeparam>
        /// <returns>An instance of the underlying structure as <typeparamref name="T"/>.</returns>
        T ToTypeOf<T>();
    }
}
