using Jolt.LanguageServer.Protocol;
using Nerdbank.Streams;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.LanguageServer.Tests
{
    /// <summary>
    /// Runs the server over the protocol, as a language client would, using an in-memory connection.
    /// </summary>
    public sealed class LanguageServerProtocolTests : IAsyncLifetime
    {
        private const string Uri = "file:///c%3A/transformers/example.jolt.json";

        private readonly BlockingCollection<PublishDiagnosticsParams> _published = new BlockingCollection<PublishDiagnosticsParams>();
        private JsonRpc _client = null!;
        private JsonRpc _serverRpc = null!;
        private JoltLanguageServer _server = null!;

        public Task InitializeAsync()
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            _serverRpc = new JsonRpc(new HeaderDelimitedMessageHandler(serverStream, serverStream, new SystemTextJsonFormatter { JsonSerializerOptions = LspJson.Options }));
            _server = new JoltLanguageServer(x => _serverRpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", x), validationDelay: TimeSpan.FromMilliseconds(10));
            _serverRpc.AddLocalRpcTarget(_server, new JsonRpcTargetOptions { NotifyClientOfEvents = false });
            _serverRpc.StartListening();

            _client = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, new SystemTextJsonFormatter { JsonSerializerOptions = LspJson.Options }));
            _client.AddLocalRpcTarget(new ClientTarget(_published));
            _client.StartListening();

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _client.Dispose();
            _serverRpc.Dispose();
            _server.Dispose();

            return Task.CompletedTask;
        }

        private sealed class ClientTarget
        {
            private readonly BlockingCollection<PublishDiagnosticsParams> _published;

            public ClientTarget(BlockingCollection<PublishDiagnosticsParams> published)
            {
                _published = published;
            }

            [JsonRpcMethod("textDocument/publishDiagnostics", UseSingleObjectParameterDeserialization = true)]
            public void PublishDiagnostics(PublishDiagnosticsParams parameters) => _published.Add(parameters);
        }

        private PublishDiagnosticsParams NextPublished()
        {
            Assert.True(_published.TryTake(out var published, TimeSpan.FromSeconds(10)), "Diagnostics were not published in time.");

            return published!;
        }

        private Task NotifyAsync(string method, object parameters) => _client.NotifyWithParameterObjectAsync(method, parameters);

        [Fact]
        public async Task Initialize_ReportsFullDocumentSync()
        {
            var result = await _client.InvokeWithParameterObjectAsync<JsonElement>("initialize", new { processId = (int?)null, capabilities = new { } });
            var sync = result.GetProperty("capabilities").GetProperty("textDocumentSync");

            Assert.True(sync.GetProperty("openClose").GetBoolean());
            Assert.Equal(1, sync.GetProperty("change").GetInt32());
        }

        [Fact]
        public async Task OpenChangeAndClose_PublishDiagnosticsForTheLatestText()
        {
            await _client.InvokeWithParameterObjectAsync<JsonElement>("initialize", new { capabilities = new { } });
            await NotifyAsync("textDocument/didOpen", new { textDocument = new { uri = Uri, languageId = "jolt", version = 1, text = "{ \"a\": \"@missing\" }" } });

            var opened = NextPublished();

            Assert.Equal(Uri, opened.Uri);
            Assert.Equal(1, opened.Version);
            Assert.Equal("JLT562", Assert.Single(opened.Diagnostics).Code);

            await NotifyAsync("textDocument/didChange", new { textDocument = new { uri = Uri, version = 2 }, contentChanges = new[] { new { text = "{ \"a\": \"1\" }" } } });

            var changed = NextPublished();

            Assert.Equal(2, changed.Version);
            Assert.Empty(changed.Diagnostics);

            await NotifyAsync("textDocument/didClose", new { textDocument = new { uri = Uri } });

            Assert.Empty(NextPublished().Diagnostics);
        }

        [Fact]
        public async Task DocumentContext_RevalidatesWithTheHostsMethodsAndTransformers()
        {
            const string text = "{ \"a\": \"#reverseString($.a)\", \"b\": \"#transformWith($.b, 'address')\" }";

            await _client.InvokeWithParameterObjectAsync<JsonElement>("initialize", new { capabilities = new { } });
            await NotifyAsync("textDocument/didOpen", new { textDocument = new { uri = Uri, languageId = "jolt", version = 1, text } });

            // Without a context, what the host provides is unknown, so neither is reported.
            Assert.Empty(NextPublished().Diagnostics);

            await NotifyAsync(JoltLanguageServer.DocumentContextNotification, new { textDocument = new { uri = Uri }, methods = new object[0], transformers = new string[0] });

            Assert.Equal(2, NextPublished().Diagnostics.Count);

            await NotifyAsync(JoltLanguageServer.DocumentContextNotification, new { textDocument = new { uri = Uri }, methods = new[] { new { name = "reverseString" } }, transformers = new[] { "address" } });

            Assert.Empty(NextPublished().Diagnostics);
        }

        [Fact]
        public async Task ConfigurationChange_AppliesTheUnsafeMethodsSetting()
        {
            await _client.InvokeWithParameterObjectAsync<JsonElement>("initialize", new { capabilities = new { } });
            await NotifyAsync("textDocument/didOpen", new { textDocument = new { uri = Uri, languageId = "jolt", version = 1, text = "{ \"a\": \"#eval('1')\" }" } });

            Assert.Single(NextPublished().Diagnostics);

            await NotifyAsync("workspace/didChangeConfiguration", new { settings = new { jolt = new { validation = new { allowUnsafeMethods = true } } } });

            Assert.Empty(NextPublished().Diagnostics);
        }
    }
}
