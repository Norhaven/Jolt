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
        /// Determines if a JSON query path can be parsed by the JSON library in use.
        /// </summary>
        /// <param name="path">The JSON query path.</param>
        /// <returns>True if the path can be used in a query, false otherwise.</returns>
        bool IsValidQueryPath(string path);

        /// <summary>
        /// Determines if a JSON query path can match several values (e.g. "$.a[*]"), in which case selecting it always gives an array of its matches.
        /// </summary>
        /// <param name="path">The JSON query path.</param>
        /// <returns>True if the path can match several values, false if it can match one at most or can't be parsed.</returns>
        bool CanMatchMultipleValues(string path);

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
