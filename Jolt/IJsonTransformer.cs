using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;

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

        /// <summary>
        /// Validates the current transformer to ensure it is properly configured and can perform transformations 
        /// without errors related to the transformer. You still may encounter errors when transforming a specific JSON input,
        /// but this method should help identify issues with the transformer itself before attempting to do so.
        /// </summary>
        /// <returns>A sequence of validation issues found during the validation process, or empty if none were found.</returns>
        IEnumerable<ValidationIssue> Validate();
    }
}
