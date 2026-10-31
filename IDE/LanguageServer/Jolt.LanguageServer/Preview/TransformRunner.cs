using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.DotNet;
using Jolt.LanguageServer.Protocol;
using Jolt.LanguageServer.Validation;
using Jolt.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jolt.LanguageServer.Preview
{
    /// <summary>
    /// Runs a transformer against a preview input file. Custom methods aren't available to the language server, so
    /// each one returns the value that the input file's mocks give it, and calling one without a mock is an error.
    /// </summary>
    internal static class TransformRunner
    {
        private const string TestDocumentGroupsProperty = "testGroups";

        // Output is shown to a person, so keep quotes, apostrophes and non-ASCII text readable rather than escaped.
        private static readonly JsonSerializerOptions _outputJson = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Only used to find out whether a name belongs to the standard library, which only reads from it.
        private static readonly Lazy<MethodReferenceResolver> _standardLibrary =
            new Lazy<MethodReferenceResolver>(() => new MethodReferenceResolver(new MessageProvider(JoltOptions.Default)));

        public static TransformResult Run(string transformerText, PreviewInput input, DocumentContext context, ValidationSettings settings)
        {
            var result = new TransformResult();

            if (input.Error != null || input.Source is null)
            {
                result.Outcome = TransformOutcome.InvalidInput;
                result.Error = new TransformError { Message = input.Error ?? "The input file can't be read." };

                return result;
            }

            if (IsTestDocument(transformerText))
            {
                result.Outcome = TransformOutcome.Error;
                result.Error = new TransformError { Message = "Jolt test files contain a transformer in each test, so they can't be previewed." };

                return result;
            }

            var standardLibraryMocks = input.Mocks.Keys.Where(IsStandardLibraryMethod).ToHashSet(StringComparer.Ordinal);
            var mocks = input.Mocks.Where(x => !standardLibraryMocks.Contains(x.Key)).ToList();

            foreach (var name in standardLibraryMocks.OrderBy(x => x, StringComparer.Ordinal))
            {
                result.Warnings.Add($"The mock for '{name}' is ignored, since '#{name}' is a standard library method and Jolt always calls it instead.");
            }

            // A mocked method is available to the transformer even when no methods.json file lists it.
            var externalMethods = context.ExternalMethods?.Union(mocks.Select(x => x.Key), StringComparer.Ordinal).ToArray();
            var validationContext = new DocumentContext(externalMethods, context.Transformers);
            var errorCount = DocumentValidator.Validate(transformerText, validationContext, settings).Count(x => x.Severity == DiagnosticSeverity.Error);

            if (errorCount > 0)
            {
                result.Outcome = TransformOutcome.Skipped;
                result.TransformerErrorCount = errorCount;

                return result;
            }

            var registrations = mocks
                .Select(x => MethodRegistration.FromDelegate(x.Key, new Func<object?[], object?>(new MockedMethod(x.Value).Invoke)))
                .Concat((context.ExternalMethods ?? Array.Empty<string>())
                    .Where(x => !input.Mocks.ContainsKey(x) && !IsStandardLibraryMethod(x))
                    .Distinct(StringComparer.Ordinal)
                    .Select(x => MethodRegistration.FromDelegate(x, new Func<object?[], object?>(new UnmockedMethod(x).Invoke))))
                .ToList();

            var options = settings.AllowUnsafeMethods ? JoltOptions.Default.WithUnsafeAllowed() : JoltOptions.Default;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var transformer = JoltJsonTransformer.DefaultWith(transformerText, null, registrations, options);
                var output = transformer.Transform(input.Source);

                result.Output = FormatOutput(output);
            }
            catch (Exception ex)
            {
                result.Outcome = TransformOutcome.Error;
                result.Error = ToTransformError(ex);
            }

            result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            return result;
        }

        private static bool IsStandardLibraryMethod(string name) => _standardLibrary.Value.GetMethod(name)?.IsSystemMethod == true;

        private static bool IsTestDocument(string text)
        {
            try
            {
                using var document = JsonDocument.Parse(text);

                return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty(TestDocumentGroupsProperty, out _);
            }
            catch (JsonException)
            {
                // Validation reports invalid JSON.
                return false;
            }
        }

        private static string? FormatOutput(string? output)
        {
            if (output is null)
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(output)?.ToJsonString(_outputJson) ?? "null";
            }
            catch (JsonException)
            {
                return output;
            }
        }

        /// <summary>
        /// Describes an exception along with the inner exceptions beneath it, since the outer one (e.g. "check inner
        /// exception for details") often hides the actual cause.
        /// </summary>
        private static TransformError ToTransformError(Exception exception)
        {
            var errors = new List<TransformError>();

            for (var current = exception; current != null; current = current.InnerException)
            {
                // Says nothing beyond that a method threw the exception within it.
                if (current is TargetInvocationException && current.InnerException != null)
                {
                    continue;
                }

                errors.Add(new TransformError
                {
                    Code = current is JoltException jolt ? GetCode(jolt.Code) : null,
                    Message = current.Message,
                });
            }

            var outermost = errors[0];
            outermost.InnerErrors.AddRange(errors.Skip(1));

            return outermost;
        }

        /// <summary>
        /// Gets Jolt's documented code for an exception code (e.g. "JLT562").
        /// </summary>
        private static string GetCode(ExceptionCode code) =>
            typeof(ExceptionCode).GetField(code.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? code.ToString();

        private sealed class MockedMethod
        {
            private readonly string _returns;

            public MockedMethod(string returns)
            {
                _returns = returns;
            }

            // A params array accepts the arguments of any call, lambdas included, which are ignored. A new token is
            // returned for each call, since a token can only be placed in one location of the output.
            public object? Invoke(params object?[] arguments) => JsonToken.Parse(_returns);
        }

        private sealed class UnmockedMethod
        {
            private readonly string _name;

            public UnmockedMethod(string name)
            {
                _name = name;
            }

            public object? Invoke(params object?[] arguments) =>
                throw new MissingMockException($"The custom method '#{_name}' has no mock in this input file, so it can't be called in the preview. Add \"{_name}\": {{ \"returns\": ... }} to the input file's \"mocks\".");
        }
    }

    internal sealed class MissingMockException : Exception
    {
        public MissingMockException(string message)
            : base(message)
        {
        }
    }
}
