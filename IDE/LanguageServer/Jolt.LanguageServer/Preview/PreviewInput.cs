using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Jolt.LanguageServer.Preview
{
    /// <summary>
    /// A preview input file (e.g. .jolt/orders/multi-item-order.input.json), which holds the source document to run a
    /// transformer against, along with the values that the transformer's custom methods return while previewing it:
    /// <code>
    /// {
    ///   "input": { ... },
    ///   "mocks": { "lookupRegion": { "returns": "EMEA" } }
    /// }
    /// </code>
    /// </summary>
    internal sealed class PreviewInput
    {
        public const string InputProperty = "input";
        public const string MocksProperty = "mocks";
        public const string ReturnsProperty = "returns";

        private PreviewInput(string? source, IReadOnlyDictionary<string, string> mocks, string? error)
        {
            Source = source;
            Mocks = mocks;
            Error = error;
        }

        /// <summary>
        /// Gets the source document's JSON, or null when the file is invalid.
        /// </summary>
        public string? Source { get; }

        /// <summary>
        /// Gets the JSON that each mocked method returns, by method name.
        /// </summary>
        public IReadOnlyDictionary<string, string> Mocks { get; }

        /// <summary>
        /// Gets why the file can't be used, or null when it can.
        /// </summary>
        public string? Error { get; }

        public static PreviewInput Parse(string text)
        {
            // Input files may hold large documents, so they're read with System.Text.Json rather than the server's own
            // JSON parser, which records the offset of every character in every string.
            try
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return Invalid("The input file must be an object with an \"input\" property.");
                }

                if (!root.TryGetProperty(InputProperty, out var input))
                {
                    return Invalid("The input file has no \"input\" property with the source document to transform.");
                }

                var mocks = new Dictionary<string, string>(StringComparer.Ordinal);

                if (root.TryGetProperty(MocksProperty, out var mocksElement))
                {
                    if (mocksElement.ValueKind != JsonValueKind.Object)
                    {
                        return Invalid("The input file's \"mocks\" property must be an object, with a property for each method.");
                    }

                    foreach (var mock in mocksElement.EnumerateObject())
                    {
                        if (mock.Value.ValueKind != JsonValueKind.Object || !mock.Value.TryGetProperty(ReturnsProperty, out var returns))
                        {
                            return Invalid($"The mock for '{mock.Name}' must be an object with a \"returns\" property, e.g. {{ \"returns\": true }}.");
                        }

                        mocks[mock.Name.TrimStart('#')] = returns.GetRawText();
                    }
                }

                return new PreviewInput(input.GetRawText(), mocks, null);
            }
            catch (JsonException ex)
            {
                var location = ex.LineNumber is long line ? $" (line {line + 1}, position {(ex.BytePositionInLine ?? 0) + 1})" : string.Empty;

                return Invalid($"The input file isn't valid JSON{location}.");
            }
        }

        private static PreviewInput Invalid(string error) => new PreviewInput(null, new Dictionary<string, string>(), error);
    }
}
