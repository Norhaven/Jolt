using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a JSON array. This can be enumerated to iterate through its elements, if any exist.
    /// </summary>
    public interface IJsonArray : IJsonToken, IEnumerable<IJsonToken>
    {
        /// <summary>
        /// Gets or sets the JSON element at the specified index of the array.
        /// </summary>
        /// <param name="index">The index in the array to use.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if present, null otherwise.</returns>
        IJsonToken? this[int index] { get; set; }

        /// <summary>
        /// Gets the number of elements that the array contains.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Removes an element by index from this array.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> which is the removed element if present, null otherwise.</returns>
        IJsonToken? RemoveAt(int index);

        /// <summary>
        /// Adds an array element to this array.
        /// </summary>
        /// <param name="token">The JSON structure to add.</param>
        void Add(IJsonToken? token);
    }
}
