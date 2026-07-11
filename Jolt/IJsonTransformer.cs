using Jolt.Structure;
using Jolt.Structure.Streaming;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        /// Transforms JSON input from a stream and writes the result to a different JSON output stream.
        /// </summary>
        /// <param name="input">The input stream containing the JSON to be transformed.</param>
        /// <param name="output">The output stream where the transformed JSON will be written.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        void TransformLines(Stream input, Stream output, StreamingOptions? options = default);

        /// <summary>
        /// Transforms JSON input from a reader and writes the result into a JSON writer.
        /// </summary>
        /// <param name="input">The input reader containing the JSON to be transformed.</param>
        /// <param name="output">The output writer where the transformed JSON will be written.</param>
        ///     
        void TransformLines(TextReader input, TextWriter output, StreamingOptions? options = default);

        /// <summary>
        /// Asynchronously transforms JSON input from a stream into a different JSON output stream.
        /// </summary>
        /// <param name="input">The input stream containing the JSON to be transformed.</param>
        /// <param name="output">The output stream where the transformed JSON will be written.</param>
        /// <param name="cancellationToken">A token to cancel the transformation operation.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TransformLinesAsync(Stream input, Stream output, CancellationToken? cancellationToken = default, StreamingOptions? options = default);

        /// <summary>
        /// Asynchronously transforms JSON input from a reader and writes the result into a JSON writer.
        /// </summary>
        /// <param name="input">The input reader containing the JSON to be transformed.</param>
        /// <param name="output">The output writer where the transformed JSON will be written.</param>
        /// <param name="cancellationToken">A token to cancel the transformation operation.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TransformLinesAsync(TextReader input, TextWriter output, CancellationToken? cancellationToken = default, StreamingOptions? options = default);

        /// <summary>
        /// Transforms a sequence of JSON input. This will automatically choose the most applicable means of transforming the individual entries, such as
        /// using objects separated by an RFC7464 record separator character which will efficiently manage the stream, whereas the worst case scenario (such as
        /// a single large array containing objects) will involve reading the entire source document into memory at once for processing.
        /// </summary>
        /// <param name="input">The input stream containing the JSON sequence to be transformed.</param>
        /// <param name="output">The output stream where the transformed JSON will be written.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        void TransformSequence(Stream input, Stream output, StreamingOptions? options = default);

        /// <summary>
        /// Transforms a sequence of JSON input. This will automatically choose the most applicable means of transforming the individual entries, such as
        /// using objects separated by an RFC7464 record separator character which will efficiently manage the stream, whereas the worst case scenario (such as
        /// a single large array containing objects) will involve reading the entire source document into memory at once for processing.
        /// </summary>
        /// <param name="input">The input stream containing the JSON sequence to be transformed.</param>
        /// <param name="output">The output stream where the transformed JSON will be written.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        void TransformSequence(TextReader reader, TextWriter writer, StreamingOptions? options = default);

        /// <summary>
        /// Transforms a sequence of JSON input. This will automatically choose the most applicable means of transforming the individual entries, such as
        /// using objects separated by an RFC7464 record separator character which will efficiently manage the stream, whereas the worst case scenario (such as
        /// a single large array containing objects) will involve reading the entire source document into memory at once for processing.
        /// </summary>
        /// <param name="input">The input stream containing the JSON sequence to be transformed.</param>
        /// <param name="output">The output stream where the transformed JSON will be written.</param>
        /// <param name="cancellationToken">A token to cancel the transformation operation.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        /// <returns>A task representing the asynchronous execution.</returns>
        Task TransformSequenceAsync(Stream input, Stream output, CancellationToken? cancellationToken = default, StreamingOptions? options = default);

        /// <summary>
        /// Transforms a sequence of JSON input. This will automatically choose the most applicable means of transforming the individual entries, such as
        /// using objects separated by an RFC7464 record separator character which will efficiently manage the stream, whereas the worst case scenario (such as
        /// a single large array containing objects) will involve reading the entire source document into memory at once for processing.
        /// </summary>
        /// <param name="input">The input stream containing the JSON sequence to be transformed.</param>
        /// <param name="output">The output stream where the transformed JSON will be written.</param>
        /// <param name="cancellationToken">A token to cancel the transformation operation.</param>
        /// <param name="options">The streaming options for the transformation.</param>
        /// <returns>A task representing the asynchronous execution.</returns>
        Task TransformSequenceAsync(TextReader reader, TextWriter writer, CancellationToken? cancellationToken = default, StreamingOptions? options = default);

        /// <summary>
        /// Validates the current transformer to ensure it is properly configured and can perform transformations 
        /// without errors related to the transformer. You still may encounter errors when transforming a specific JSON input,
        /// but this method should help identify issues with the transformer itself before attempting to do so.
        /// </summary>
        /// <returns>A sequence of validation issues found during the validation process, or empty if none were found.</returns>
        IEnumerable<ValidationIssue> Validate();
    }
}
