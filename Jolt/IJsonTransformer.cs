using System;

namespace Jolt
{
    /// <summary>
    /// Represents a JSON transformer.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IJsonTransformer<out TContext> where TContext : IJsonContext
    {
        /// <summary>
        /// Transforms JSON input into a different JSON output.
        /// </summary>
        /// <param name="json">The JSON string to be transformed.</param>
        /// <returns>The transformed JSON string.</returns>
        string? Transform(string json);
    }
}
