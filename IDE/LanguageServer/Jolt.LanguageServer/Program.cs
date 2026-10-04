using Jolt.LanguageServer.Protocol;
using StreamJsonRpc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Jolt.LanguageServer
{
    /// <summary>
    /// A Language Server Protocol server for Jolt transformers, which validates them with the Jolt library. It
    /// communicates over standard input and output, so any editor with a language client can start and use it.
    /// </summary>
    /// <remarks>
    /// Usage: dotnet Jolt.LanguageServer.dll [--stdio]
    /// </remarks>
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var input = Console.OpenStandardInput();
            var output = Console.OpenStandardOutput();

            // Standard output carries the protocol, so anything else written to the console goes to standard error,
            // which clients show as the server's log.
            Console.SetOut(Console.Error);

            var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = LspJson.Options };

            using var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(output, input, formatter));

            // The exit notification ends the process. Disposing the connection from within its own handler would wait
            // for that handler to finish, so the process exits directly instead.
            using var server = new JoltLanguageServer(
                x => rpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", x),
                Environment.Exit);

            rpc.AddLocalRpcTarget(server, new JsonRpcTargetOptions { NotifyClientOfEvents = false });
            rpc.StartListening();

            try
            {
                await rpc.Completion.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ConnectionLostException || ex is IOException || ex is ObjectDisposedException)
            {
                // The client closed the connection.
            }

            // The connection ended without an exit notification, e.g. because the client process ended.
            return 1;
        }
    }
}
