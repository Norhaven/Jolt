using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a way of querying a JSON structure for results.
    /// </summary>
    public interface IQueryPathProvider
    {
        /// <summary>
        /// Determines if a string represents a JSON query path.
        /// </summary>
        /// <param name="path">The JSON query path.</param>
        /// <returns>True if the path represents a valid JSON query path, false otherwise.</returns>
        bool IsQueryPath(string path);

        /// <summary>
        /// Selects a JSON structure from the source based on the query path and mode provided.
        /// </summary>
        /// <param name="closureSources">The potentially applicable JSON structures to query.</param>
        /// <param name="path">The JSON query path.</param>
        /// <param name="queryMode">The JSON query mode.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the query resulted in a valid JSON structure, null otherwise.</returns>
        IJsonToken? SelectNodeAtPath(IEnumerable<IJsonToken> closureSources, string path, JsonQueryMode queryMode);

        /// <summary>
        /// Gets the root JSON structure from a series of potentially applicable JSON structures based on the mode provided.
        /// </summary>
        /// <param name="closureSources">The potentially applicable JSON structures to query.</param>
        /// <param name="queryMode">The JSON query mode.</param>
        /// <returns>An instance of <see cref="IJsonToken"/> if the query resulted in a valid JSON structure, null otherwise.</returns>
        IJsonToken? GetRootNodeFrom(IEnumerable<IJsonToken> closureSources, JsonQueryMode queryMode);
    }
}
