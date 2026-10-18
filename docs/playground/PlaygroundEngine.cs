using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.DotNet;
using Jolt.LanguageServer.Mapping;
using Jolt.LanguageServer.Protocol;
using Jolt.LanguageServer.Validation;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Jolt.Playground
{
    /// <summary>
    /// The playground's interface to JavaScript. Values cross the boundary as JSON strings, so the JavaScript side
    /// only ever deals with plain objects. See docs/public/playground/engine-worker.js for the caller.
    /// </summary>
    [SupportedOSPlatform("browser")]
    public static partial class PlaygroundEngine
    {
        private static readonly JsonSerializerOptions _resultJson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        // Output is shown to a person, so keep quotes, apostrophes and non-ASCII text readable rather than escaped.
        private static readonly JsonSerializerOptions _outputJson = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly string[] _externalMethodNames = PlaygroundMethods.Descriptions.Select(x => x.Name).ToArray();

        /// <summary>
        /// Describes the engine: the Jolt version and the external methods the playground registers.
        /// </summary>
        [JSExport]
        public static string Describe()
        {
            var version = typeof(JoltContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(JoltContext).Assembly.GetName().Version?.ToString()
                ?? "unknown";

            // Drop the "+<commit>" suffix the SDK appends to informational versions.
            var plus = version.IndexOf('+');
            var shortVersion = plus > 0 ? version.Substring(0, plus) : version;

            return JsonSerializer.Serialize(new EngineDescription
            {
                Version = shortVersion,
                ExternalMethods = PlaygroundMethods.Descriptions.Select(x => new ExternalMethodDescription { Name = x.Name, Signature = x.Signature }).ToList(),
            }, _resultJson);
        }

        /// <summary>
        /// Validates a transformer and, when it has no errors, runs it against a source document.
        /// </summary>
        /// <param name="transformerText">The transformer JSON, as typed.</param>
        /// <param name="sourceText">The source document JSON, as typed.</param>
        /// <param name="optionsJson">A serialized <see cref="RunOptions"/>.</param>
        /// <returns>A serialized <see cref="RunResult"/>.</returns>
        [JSExport]
        public static string Run(string transformerText, string sourceText, string optionsJson)
        {
            var options = JsonSerializer.Deserialize<RunOptions>(optionsJson, _resultJson) ?? new RunOptions();
            var result = new RunResult();

            try
            {
                result.TransformerDiagnostics = ValidateTransformer(transformerText, options);
                result.SourceDiagnostics = ValidateSource(sourceText);

                if (result.TransformerDiagnostics.Any(x => x.Severity == "error") || result.SourceDiagnostics.Count > 0)
                {
                    result.Skipped = true;
                    return JsonSerializer.Serialize(result, _resultJson);
                }

                Transform(transformerText, sourceText, options, result);
            }
            catch (Exception ex)
            {
                // Anything unexpected is still reported in the playground rather than lost in the worker.
                result.Error = new RunError { Message = $"{ex.GetType().Name}: {ex.Message}" };
            }

            return JsonSerializer.Serialize(result, _resultJson);
        }

        private static List<PlaygroundDiagnostic> ValidateTransformer(string text, RunOptions options)
        {
            // An empty transformer list means #transformWith calls to any name are reported, since none are registered.
            var context = new DocumentContext(_externalMethodNames, Array.Empty<string>());
            var diagnostics = DocumentValidator.Validate(text, context, new ValidationSettings(options.AllowUnsafe));
            var lines = new LineIndex(text);
            var unknownMethodCode = GetCode(ExceptionCode.UnableToFindMethodImplementation);

            return diagnostics.Select(x =>
            {
                var from = lines.GetOffset(x.Range.Start);
                var to = lines.GetOffset(x.Range.End);
                var message = x.Message;

                // The language server only suggests a library method when it doesn't know the host's methods. The
                // playground knows them, so an unknown name is an error, but a suggestion still helps with typos.
                if (x.Code == unknownMethodCode && to > from)
                {
                    var name = text.Substring(from, to - from).TrimStart('#');
                    var suggestion = SimilarLibraryMethods.FindClosest(name);

                    if (suggestion != null)
                    {
                        message += $" Did you mean '#{suggestion}'?";
                    }
                }

                return new PlaygroundDiagnostic
                {
                    From = from,
                    To = to,
                    Severity = ToSeverity(x.Severity),
                    Code = x.Code,
                    Message = message,
                };
            }).ToList();
        }

        private static List<PlaygroundDiagnostic> ValidateSource(string text)
        {
            try
            {
                JsonSyntaxParser.Parse(text);
                return new List<PlaygroundDiagnostic>();
            }
            catch (JsonSyntaxException ex)
            {
                return new List<PlaygroundDiagnostic>
                {
                    new PlaygroundDiagnostic
                    {
                        From = ex.Offset,
                        To = Math.Min(ex.Offset + 1, text.Length),
                        Severity = "error",
                        Message = $"Invalid JSON: {ex.Message}",
                    },
                };
            }
        }

        private static void Transform(string transformerText, string sourceText, RunOptions options, RunResult result)
        {
            var joltOptions = JoltOptions.Default;

            if (options.LooseErrorHandling)
            {
                joltOptions = joltOptions.WithLooseErrorHandling();
            }

            if (options.AllowUnsafe)
            {
                joltOptions = joltOptions.WithUnsafeAllowed();
            }

            if (options.Trace)
            {
                joltOptions = joltOptions.WithExecutionTracing();
            }

            // Built by hand (as JoltJsonTransformer.DefaultWith does) to keep hold of the message provider, which is
            // where execution traces are collected.
            var messageProvider = new MessageProvider(joltOptions);
            var context = new JoltContext(
                transformerText,
                new ExpressionParser(),
                new ExpressionEvaluator(joltOptions),
                new TokenReader(messageProvider),
                new JsonTokenReader(),
                new JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(joltOptions),
                new PlaygroundMethods());

            context.RegisterAllMethodsFrom<PlaygroundMethods>();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var transformer = new JoltJsonTransformer(context);
                var output = transformer.Transform(sourceText);

                stopwatch.Stop();
                result.Output = FormatOutput(output);
            }
            catch (JoltException ex)
            {
                stopwatch.Stop();
                result.Error = new RunError { Code = GetCode(ex.Code), Message = ex.Message };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Error = new RunError { Message = ex.Message };
            }

            result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            if (options.Trace)
            {
                result.Trace = FormatTraces(messageProvider.ExecutionTraces);
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

        private static string FormatTraces(ExecutionTraceEntry[] entries)
        {
            // ExecutionTraces lists every entry, including nested ones, so print only the roots (each prints its children).
            var nested = new HashSet<ExecutionTraceEntry>();
            var pending = new Stack<ExecutionTraceEntry>(entries);

            while (pending.Count > 0)
            {
                foreach (var child in pending.Pop().ChildEntries)
                {
                    if (nested.Add(child))
                    {
                        pending.Push(child);
                    }
                }
            }

            return string.Join(Environment.NewLine, entries.Where(x => !nested.Contains(x)).Select(x => x.ToString()));
        }

        private static string ToSeverity(DiagnosticSeverity severity) => severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info",
        };

        /// <summary>
        /// Gets Jolt's documented code for an exception code (e.g. "JLT562").
        /// </summary>
        private static string GetCode(ExceptionCode code) =>
            typeof(ExceptionCode).GetField(code.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? code.ToString();

        /// <summary>
        /// Converts line and character positions back to offsets, which is what the editor works with.
        /// </summary>
        private sealed class LineIndex
        {
            private readonly List<int> _lineStarts = new List<int> { 0 };
            private readonly int _length;

            public LineIndex(string text)
            {
                _length = text.Length;

                // The same line endings as the language server's TextLines.
                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                    {
                        _lineStarts.Add(i + 1);
                    }
                }
            }

            public int GetOffset(Position position)
            {
                var line = Math.Max(0, Math.Min(position.Line, _lineStarts.Count - 1));

                return Math.Min(_lineStarts[line] + position.Character, _length);
            }
        }
    }

    public sealed class RunOptions
    {
        public bool LooseErrorHandling { get; set; }

        public bool AllowUnsafe { get; set; }

        public bool Trace { get; set; }
    }

    public sealed class RunResult
    {
        public List<PlaygroundDiagnostic> TransformerDiagnostics { get; set; } = new List<PlaygroundDiagnostic>();

        public List<PlaygroundDiagnostic> SourceDiagnostics { get; set; } = new List<PlaygroundDiagnostic>();

        /// <summary>
        /// Gets or sets whether the transformation didn't run because of errors in either document.
        /// </summary>
        public bool Skipped { get; set; }

        public string? Output { get; set; }

        public RunError? Error { get; set; }

        public double? ElapsedMilliseconds { get; set; }

        public string? Trace { get; set; }
    }

    public sealed class RunError
    {
        public string? Code { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    public sealed class PlaygroundDiagnostic
    {
        public int From { get; set; }

        public int To { get; set; }

        public string Severity { get; set; } = "error";

        public string? Code { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    public sealed class EngineDescription
    {
        public string Version { get; set; } = string.Empty;

        public List<ExternalMethodDescription> ExternalMethods { get; set; } = new List<ExternalMethodDescription>();
    }

    public sealed class ExternalMethodDescription
    {
        public string Name { get; set; } = string.Empty;

        public string Signature { get; set; } = string.Empty;
    }
}
