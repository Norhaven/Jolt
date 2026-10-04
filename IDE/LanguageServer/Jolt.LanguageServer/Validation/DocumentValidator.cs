using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.DotNet;
using Jolt.LanguageServer.Mapping;
using Jolt.LanguageServer.Protocol;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Jolt.LanguageServer.Validation
{
    /// <summary>
    /// Describes what the host application provides to a transformer. A null list means that it is not known, in which
    /// case issues that depend on it (unknown methods or transformers) are not reported.
    /// </summary>
    internal sealed class DocumentContext
    {
        public static DocumentContext Unknown { get; } = new DocumentContext(null, null);

        public DocumentContext(IReadOnlyCollection<string>? externalMethods, IReadOnlyCollection<string>? transformers)
        {
            ExternalMethods = externalMethods;
            Transformers = transformers;
        }

        public IReadOnlyCollection<string>? ExternalMethods { get; }

        public IReadOnlyCollection<string>? Transformers { get; }
    }

    internal sealed class ValidationSettings
    {
        public static ValidationSettings Default { get; } = new ValidationSettings(false);

        public ValidationSettings(bool allowUnsafeMethods)
        {
            AllowUnsafeMethods = allowUnsafeMethods;
        }

        /// <summary>
        /// Gets whether the host application enables unsafe methods (e.g. #eval), so that using them is not reported.
        /// </summary>
        public bool AllowUnsafeMethods { get; }
    }

    /// <summary>
    /// Validates a transformer document with Jolt and reports the issues as diagnostics located within the document.
    /// </summary>
    internal static class DocumentValidator
    {
        private const string TestDocumentGroupsProperty = "testGroups";

        private static readonly ConcurrentDictionary<ExceptionCode, string> _codes = new ConcurrentDictionary<ExceptionCode, string>();

        public static List<Diagnostic> Validate(string text, DocumentContext context, ValidationSettings settings)
        {
            var lines = new TextLines(text);
            JsonSyntaxNode root;

            try
            {
                root = JsonSyntaxParser.Parse(text);
            }
            catch (JsonSyntaxException ex)
            {
                // Language clients don't otherwise check the JSON in a Jolt document, and Jolt can't validate it until it parses.
                return new List<Diagnostic>
                {
                    new Diagnostic
                    {
                        Range = lines.GetRange(ex.Offset, Math.Min(ex.Offset + 1, text.Length)),
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Invalid JSON: {ex.Message}"
                    }
                };
            }

            return GetTransformers(root)
                .SelectMany(x => ValidateTransformer(text, x, lines, context, settings))
                .ToList();
        }

        /// <summary>
        /// Gets the transformers in a document. This is usually the whole document, but a Jolt test document (with a
        /// "testGroups" array) contains a transformer in each test, alongside source documents that are not transformers.
        /// </summary>
        private static IEnumerable<JsonSyntaxNode> GetTransformers(JsonSyntaxNode root)
        {
            if (!(root is JsonObjectSyntax document) || !(document[TestDocumentGroupsProperty] is JsonArraySyntax groups))
            {
                return new[] { root };
            }

            return from @group in groups.Items.OfType<JsonObjectSyntax>()
                   let tests = @group["tests"] as JsonArraySyntax
                   where tests != null
                   from test in tests.Items.OfType<JsonObjectSyntax>()
                   let transformer = test["transformer"]
                   where transformer != null
                   select transformer;
        }

        private static IEnumerable<Diagnostic> ValidateTransformer(string text, JsonSyntaxNode transformer, TextLines lines, DocumentContext context, ValidationSettings settings)
        {
            var transformerText = text.Substring(transformer.Start, transformer.End - transformer.Start);
            List<ValidationIssue> issues;

            try
            {
                var token = JsonToken.Parse(transformerText);

                if (token is null)
                {
                    return Array.Empty<Diagnostic>();
                }

                issues = new JoltTransformerValidator<JoltContext>(CreateContext(transformerText, context)).Validate(token).ToList();
            }
            catch (Exception ex)
            {
                // E.g. duplicate property names, which are valid JSON but which System.Text.Json (and so Jolt) rejects.
                return new[]
                {
                    new Diagnostic
                    {
                        Range = lines.GetRange(transformer.Start, transformer.Start + 1),
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Unable to validate this transformer: {ex.Message}"
                    }
                };
            }

            var index = new JsonPathIndex(transformer);
            var occurrences = new OccurrenceTracker();

            return issues
                .Where(x => IsReported(x, context, settings))
                .Select(x => ToDiagnostic(x, index, lines, occurrences))
                .ToList();
        }

        private static JoltContext CreateContext(string transformerText, DocumentContext documentContext)
        {
            var options = JoltOptions.Default;
            var messageProvider = new MessageProvider(options);

            var context = new JoltContext(
                transformerText,
                new ExpressionParser(),
                new ExpressionEvaluator(options),
                new TokenReader(messageProvider),
                new JsonTokenReader(),
                new JsonPathQueryPathProvider(),
                new ExternalMethodResolver(documentContext.ExternalMethods ?? Array.Empty<string>()),
                messageProvider,
                new ErrorHandler(options));

            // Only the names matter for validation, since referenced transformers are not validated from here.
            context.RegisterAllTransformers((documentContext.Transformers ?? Array.Empty<string>()).Select(x => new TransformerRegistration(x, "{}")));

            return context;
        }

        private static bool IsReported(ValidationIssue issue, DocumentContext context, ValidationSettings settings)
        {
            switch (issue.Type)
            {
                case ValidationIssueType.UnsafeMethodUsage:
                    return !settings.AllowUnsafeMethods;
                case ValidationIssueType.MissingTransformerReference:
                    return context.Transformers != null;
                case ValidationIssueType.SyntaxError when issue.Code == ExceptionCode.UnableToFindMethodImplementation:
                    return context.ExternalMethods != null;
                default:
                    return true;
            }
        }

        private static Diagnostic ToDiagnostic(ValidationIssue issue, JsonPathIndex index, TextLines lines, OccurrenceTracker occurrences)
        {
            var (start, end) = Locate(issue, index, occurrences);

            return new Diagnostic
            {
                Range = lines.GetRange(start, end),
                Severity = issue.Type == ValidationIssueType.UnsafeMethodUsage ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error,
                Code = GetCode(issue.Code),
                Message = GetMessage(issue)
            };
        }

        /// <summary>
        /// Locates an issue within the document. Jolt reports the path to the property or array element with the
        /// issue, so that finds the string containing the expression. Within it, a syntax error covers the whole
        /// expression, while other issues cover the method or variable they are about where it can be found.
        /// </summary>
        private static (int Start, int End) Locate(ValidationIssue issue, JsonPathIndex index, OccurrenceTracker occurrences)
        {
            var entry = index.Find(issue.TransformerExpressionPath);
            var expression = (issue.IsInPropertyName ? entry?.Key : entry?.Value as JsonStringSyntax)
                ?? FindExpression(issue, index);

            if (expression is null)
            {
                return (index.Root.Start, index.Root.Start + 1);
            }

            var needle = issue.Type == ValidationIssueType.SyntaxError ? null : GetNeedle(issue);
            var found = needle is null ? -1 : occurrences.FindNext(expression, needle);

            if (found < 0)
            {
                // The whole expression, without its quotes.
                return (expression.Start + 1, expression.End - 1);
            }

            return (expression.RawOffsets[found], expression.RawOffsets[found + needle!.Length]);
        }

        /// <summary>
        /// Finds the expression with an issue when its path cannot be used, e.g. for a property whose value is null,
        /// which Jolt reports with an empty path.
        /// </summary>
        private static JsonStringSyntax? FindExpression(ValidationIssue issue, JsonPathIndex index)
        {
            if (string.IsNullOrEmpty(issue.ExpressionText))
            {
                return null;
            }

            if (issue.Type == ValidationIssueType.SyntaxError)
            {
                return index.Strings.FirstOrDefault(x => x.Value == issue.ExpressionText);
            }

            var needle = GetNeedle(issue);

            return needle is null ? null : index.Strings.FirstOrDefault(x => OccurrenceTracker.IndexOf(x.Value, needle, 0) >= 0);
        }

        /// <summary>
        /// Gets the text to find within an expression for an issue about a specific method, variable, or transformer.
        /// </summary>
        private static string? GetNeedle(ValidationIssue issue)
        {
            var text = issue.ExpressionText;

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            switch (issue.Type)
            {
                case ValidationIssueType.UndeclaredVariable:
                    return text.StartsWith("@", StringComparison.Ordinal) ? text : "@" + text;
                case ValidationIssueType.InvalidMethodContext:
                case ValidationIssueType.UnsafeMethodUsage:
                case ValidationIssueType.ArgumentCountMismatch:
                case ValidationIssueType.UnknownMethod:
                    return "#" + text;
                case ValidationIssueType.MissingTransformerReference:
                    return "'" + text + "'";
                default:
                    return null;
            }
        }

        private static string GetMessage(ValidationIssue issue)
        {
            // Syntax error messages start with the path and expression, which a diagnostic already shows by its location.
            var prefix = $"Error parsing expression at transformer path '{issue.TransformerExpressionPath}' with expression '{issue.ExpressionText}': ";

            return issue.Message.StartsWith(prefix, StringComparison.Ordinal) ? issue.Message.Substring(prefix.Length) : issue.Message;
        }

        /// <summary>
        /// Gets the documented code for an exception code (e.g. "JLT562"), which is how Jolt identifies its errors.
        /// </summary>
        private static string GetCode(ExceptionCode code) => _codes.GetOrAdd(code, x =>
            typeof(ExceptionCode).GetField(x.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? x.ToString());

        /// <summary>
        /// Tracks which occurrence of a method or variable each issue refers to, so that repeated issues about the same
        /// name within an expression (which Jolt reports in order) are placed on successive occurrences.
        /// </summary>
        private sealed class OccurrenceTracker
        {
            private readonly Dictionary<(JsonStringSyntax, string), int> _nextSearchStart = new Dictionary<(JsonStringSyntax, string), int>();

            public int FindNext(JsonStringSyntax expression, string needle)
            {
                _nextSearchStart.TryGetValue((expression, needle), out var searchStart);

                var found = IndexOf(expression.Value, needle, searchStart);

                // When there are more issues than occurrences, the remaining issues share the first occurrence.
                if (found < 0 && searchStart > 0)
                {
                    found = IndexOf(expression.Value, needle, 0);
                }

                if (found >= 0)
                {
                    _nextSearchStart[(expression, needle)] = found + needle.Length;
                }

                return found;
            }

            /// <summary>
            /// Finds a method or variable name, rather than the start of a longer name (e.g. "@x" within "@xs").
            /// </summary>
            public static int IndexOf(string text, string needle, int start)
            {
                var requiresBoundary = needle[0] == '#' || needle[0] == '@';

                for (var found = text.IndexOf(needle, start, StringComparison.Ordinal); found >= 0; found = text.IndexOf(needle, found + 1, StringComparison.Ordinal))
                {
                    var after = found + needle.Length;

                    if (!requiresBoundary || after >= text.Length || !(char.IsLetterOrDigit(text[after]) || text[after] == '_'))
                    {
                        return found;
                    }
                }

                return -1;
            }
        }
    }
}
