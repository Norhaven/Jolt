using Jolt.LanguageServer.Protocol;
using Jolt.LanguageServer.Validation;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jolt.LanguageServer
{
    /// <summary>
    /// Handles the Language Server Protocol messages for Jolt transformer documents. Diagnostics are published for each
    /// open document shortly after it is opened or changed, and again when its context or the settings change.
    /// </summary>
    internal sealed class JoltLanguageServer : IDisposable
    {
        public const string DocumentContextNotification = "jolt/didChangeDocumentContext";

        private static readonly TimeSpan DefaultValidationDelay = TimeSpan.FromMilliseconds(250);

        private readonly object _lock = new object();
        private readonly Dictionary<string, OpenDocument> _documents = new Dictionary<string, OpenDocument>(StringComparer.Ordinal);
        private readonly Dictionary<string, DocumentContext> _contexts = new Dictionary<string, DocumentContext>(StringComparer.Ordinal);
        private readonly Func<PublishDiagnosticsParams, Task> _publish;
        private readonly Action<int>? _exit;
        private readonly TimeSpan _validationDelay;
        private ValidationSettings _settings = ValidationSettings.Default;
        private bool _isShutdownRequested;

        public JoltLanguageServer(Func<PublishDiagnosticsParams, Task> publish, Action<int>? exit = null, TimeSpan? validationDelay = null)
        {
            _publish = publish;
            _exit = exit;
            _validationDelay = validationDelay ?? DefaultValidationDelay;
        }

        [JsonRpcMethod("initialize", UseSingleObjectParameterDeserialization = true)]
        public InitializeResult Initialize(InitializeParams parameters)
        {
            // Settings use the same shape in the initialization options as in configuration changes under "jolt".
            lock (_lock)
            {
                _settings = ReadSettings(parameters.InitializationOptions, _settings);
            }

            return new InitializeResult
            {
                ServerInfo = new ServerInfo { Version = typeof(JoltLanguageServer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion }
            };
        }

        [JsonRpcMethod("initialized")]
        public void Initialized()
        {
        }

        [JsonRpcMethod("shutdown")]
        public object? Shutdown()
        {
            lock (_lock)
            {
                _isShutdownRequested = true;

                foreach (var document in _documents.Values)
                {
                    document.CancelValidation();
                }
            }

            return null;
        }

        [JsonRpcMethod("exit")]
        public void Exit()
        {
            bool isShutdownRequested;

            lock (_lock)
            {
                isShutdownRequested = _isShutdownRequested;
            }

            // The protocol asks for a non-zero exit code when the client exits without shutting down first.
            _exit?.Invoke(isShutdownRequested ? 0 : 1);
        }

        [JsonRpcMethod("textDocument/didOpen", UseSingleObjectParameterDeserialization = true)]
        public void DidOpen(DidOpenTextDocumentParams parameters)
        {
            var item = parameters.TextDocument;

            lock (_lock)
            {
                if (_documents.TryGetValue(item.Uri, out var existing))
                {
                    existing.CancelValidation();
                }

                _documents[item.Uri] = new OpenDocument(item.Uri, item.Version, item.Text);
                ScheduleValidation(item.Uri, TimeSpan.Zero);
            }
        }

        [JsonRpcMethod("textDocument/didChange", UseSingleObjectParameterDeserialization = true)]
        public void DidChange(DidChangeTextDocumentParams parameters)
        {
            var identifier = parameters.TextDocument;

            // Full document sync sends the whole text as the last (and only) change.
            var text = parameters.ContentChanges.LastOrDefault()?.Text;

            lock (_lock)
            {
                // Ignore changes that arrive out of order, so a newer text is never replaced by an older one.
                if (text is null || !_documents.TryGetValue(identifier.Uri, out var document) || identifier.Version < document.Version)
                {
                    return;
                }

                document.Version = identifier.Version;
                document.Text = text;
                ScheduleValidation(identifier.Uri, _validationDelay);
            }
        }

        [JsonRpcMethod("textDocument/didClose", UseSingleObjectParameterDeserialization = true)]
        public void DidClose(DidCloseTextDocumentParams parameters)
        {
            var uri = parameters.TextDocument.Uri;

            lock (_lock)
            {
                if (_documents.TryGetValue(uri, out var document))
                {
                    document.CancelValidation();
                    _documents.Remove(uri);
                }

                _contexts.Remove(uri);
            }

            // Diagnostics for a closed document are cleared, as it may be changed or deleted elsewhere.
            _ = PublishAsync(new PublishDiagnosticsParams { Uri = uri });
        }

        [JsonRpcMethod("workspace/didChangeConfiguration", UseSingleObjectParameterDeserialization = true)]
        public void DidChangeConfiguration(DidChangeConfigurationParams parameters)
        {
            lock (_lock)
            {
                var settings = parameters.Settings is JsonElement element && element.ValueKind == JsonValueKind.Object && element.TryGetProperty("jolt", out var jolt)
                    ? jolt
                    : parameters.Settings;

                _settings = ReadSettings(settings, _settings);
                ScheduleAllValidations();
            }
        }

        [JsonRpcMethod(DocumentContextNotification, UseSingleObjectParameterDeserialization = true)]
        public void DidChangeDocumentContext(DocumentContextParams parameters)
        {
            var uri = parameters.TextDocument.Uri;

            lock (_lock)
            {
                _contexts[uri] = new DocumentContext(
                    parameters.Methods?.Select(x => x.Name).Where(x => !string.IsNullOrEmpty(x)).ToArray(),
                    parameters.Transformers?.Where(x => !string.IsNullOrEmpty(x)).ToArray());

                if (_documents.ContainsKey(uri))
                {
                    ScheduleValidation(uri, TimeSpan.Zero);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var document in _documents.Values)
                {
                    document.CancelValidation();
                }

                _documents.Clear();
            }
        }

        private void ScheduleAllValidations()
        {
            foreach (var uri in _documents.Keys)
            {
                ScheduleValidation(uri, TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Validates a document after a delay, replacing any validation of it that has not finished, so that only the
        /// latest text is published while it is being typed. Must be called while holding the lock.
        /// </summary>
        private void ScheduleValidation(string uri, TimeSpan delay)
        {
            if (_isShutdownRequested || !_documents.TryGetValue(uri, out var document))
            {
                return;
            }

            var cancellation = document.RestartValidation();
            var text = document.Text;
            var version = document.Version;
            var context = _contexts.TryGetValue(uri, out var documentContext) ? documentContext : DocumentContext.Unknown;
            var settings = _settings;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellation).ConfigureAwait(false);
                    }

                    var diagnostics = DocumentValidator.Validate(text, context, settings);

                    cancellation.ThrowIfCancellationRequested();

                    await PublishAsync(new PublishDiagnosticsParams { Uri = uri, Version = version, Diagnostics = diagnostics }).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // A newer validation of the document replaced this one.
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to validate {uri}: {ex}");
                }
            });
        }

        private async Task PublishAsync(PublishDiagnosticsParams parameters)
        {
            try
            {
                await _publish(parameters).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ConnectionLostException || ex is ObjectDisposedException)
            {
                // The client has gone away, so there is no one to publish to.
            }
        }

        private static ValidationSettings ReadSettings(JsonElement? settings, ValidationSettings current)
        {
            if (!(settings is JsonElement element)
                || element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty("validation", out var validation)
                || validation.ValueKind != JsonValueKind.Object)
            {
                return current;
            }

            var allowUnsafeMethods = validation.TryGetProperty("allowUnsafeMethods", out var allowUnsafe) && allowUnsafe.ValueKind == JsonValueKind.True;

            return new ValidationSettings(allowUnsafeMethods);
        }

        private sealed class OpenDocument
        {
            private CancellationTokenSource? _validation;

            public OpenDocument(string uri, int version, string text)
            {
                Uri = uri;
                Version = version;
                Text = text;
            }

            public string Uri { get; }

            public int Version { get; set; }

            public string Text { get; set; }

            public CancellationToken RestartValidation()
            {
                CancelValidation();
                _validation = new CancellationTokenSource();

                return _validation.Token;
            }

            public void CancelValidation()
            {
                _validation?.Cancel();
                _validation?.Dispose();
                _validation = null;
            }
        }
    }
}
