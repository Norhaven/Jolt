using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jolt.LanguageServer.Protocol
{
    // The subset of the Language Server Protocol (https://microsoft.github.io/language-server-protocol/) that this
    // server uses. Property names are serialized in camel case, as the protocol requires.

    internal static class LspJson
    {
        public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    internal sealed class InitializeParams
    {
        public JsonElement? InitializationOptions { get; set; }
    }

    internal sealed class InitializeResult
    {
        public ServerCapabilities Capabilities { get; set; } = new ServerCapabilities();

        public ServerInfo ServerInfo { get; set; } = new ServerInfo();
    }

    internal sealed class ServerCapabilities
    {
        public TextDocumentSyncOptions TextDocumentSync { get; set; } = new TextDocumentSyncOptions();
    }

    internal sealed class TextDocumentSyncOptions
    {
        public bool OpenClose { get; set; } = true;

        /// <summary>
        /// Gets or sets how changes are sent. 1 is full document sync, which keeps the server simple since
        /// transformers are small and every change requires validating the whole document anyway.
        /// </summary>
        public int Change { get; set; } = 1;
    }

    internal sealed class ServerInfo
    {
        public string Name { get; set; } = "Jolt Language Server";

        public string? Version { get; set; }
    }

    internal sealed class TextDocumentIdentifier
    {
        public string Uri { get; set; } = string.Empty;
    }

    internal sealed class TextDocumentItem
    {
        public string Uri { get; set; } = string.Empty;

        public string LanguageId { get; set; } = string.Empty;

        public int Version { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    internal sealed class VersionedTextDocumentIdentifier
    {
        public string Uri { get; set; } = string.Empty;

        public int Version { get; set; }
    }

    internal sealed class DidOpenTextDocumentParams
    {
        public TextDocumentItem TextDocument { get; set; } = new TextDocumentItem();
    }

    internal sealed class DidChangeTextDocumentParams
    {
        public VersionedTextDocumentIdentifier TextDocument { get; set; } = new VersionedTextDocumentIdentifier();

        public List<TextDocumentContentChangeEvent> ContentChanges { get; set; } = new List<TextDocumentContentChangeEvent>();
    }

    internal sealed class TextDocumentContentChangeEvent
    {
        public string Text { get; set; } = string.Empty;
    }

    internal sealed class DidCloseTextDocumentParams
    {
        public TextDocumentIdentifier TextDocument { get; set; } = new TextDocumentIdentifier();
    }

    internal sealed class DidChangeConfigurationParams
    {
        public JsonElement? Settings { get; set; }
    }

    internal sealed class PublishDiagnosticsParams
    {
        public string Uri { get; set; } = string.Empty;

        public int? Version { get; set; }

        public List<Diagnostic> Diagnostics { get; set; } = new List<Diagnostic>();
    }

    internal sealed class Diagnostic
    {
        public Range Range { get; set; } = new Range();

        public DiagnosticSeverity Severity { get; set; }

        public string? Code { get; set; }

        public string Source { get; set; } = "Jolt";

        public string Message { get; set; } = string.Empty;
    }

    internal enum DiagnosticSeverity
    {
        Error = 1,
        Warning = 2,
        Information = 3,
        Hint = 4
    }

    internal sealed class Range
    {
        public Position Start { get; set; } = new Position();

        public Position End { get; set; } = new Position();
    }

    /// <summary>
    /// Represents a zero-based position, where the character is counted in UTF-16 code units (the protocol default).
    /// </summary>
    internal sealed class Position
    {
        public int Line { get; set; }

        public int Character { get; set; }
    }

    /// <summary>
    /// Parameters of the Jolt-specific "jolt/didChangeDocumentContext" notification, which a client sends to describe
    /// what the host application provides to a transformer. Each list is optional: when it is absent (null), the
    /// server does not know what the host provides and so does not report issues that depend on it, such as unknown
    /// methods or transformers. An empty list means that the host provides nothing.
    /// </summary>
    internal sealed class DocumentContextParams
    {
        public TextDocumentIdentifier TextDocument { get; set; } = new TextDocumentIdentifier();

        public List<ExternalMethod>? Methods { get; set; }

        public List<string>? Transformers { get; set; }
    }

    internal sealed class ExternalMethod
    {
        public string Name { get; set; } = string.Empty;
    }
}
