# Jolt Language Server

A [Language Server Protocol](https://microsoft.github.io/language-server-protocol/) server for Jolt transformers. It validates transformers with the Jolt library (the same validation as `JoltTransformer.Validate()`) and publishes the issues as diagnostics, so any editor with a language client can use it. The VS Code extension in `IDE/Extensions/VSCode/jolt` is its first client.

## Building and running

The server targets .NET 8 and rolls forward to any later runtime.

```
dotnet publish IDE/LanguageServer/Jolt.LanguageServer -c Release -o <output folder>
dotnet <output folder>/Jolt.LanguageServer.dll
```

It communicates over standard input and output (an optional `--stdio` argument is accepted and ignored), and writes its log to standard error. The tests are run with `dotnet test IDE/LanguageServer/Jolt.LanguageServer.Tests`.

The VS Code extension publishes the server into its `server` folder with `npm run build-server`, which also runs when the extension is packaged.

## Protocol

The server uses full document sync (`textDocumentSync.change` is 1) and handles these standard messages:

| Message | Behavior |
| ------- | -------- |
| `initialize` | Accepts settings as `initializationOptions` (see below). |
| `textDocument/didOpen`, `didChange` | Validates the document and publishes `textDocument/publishDiagnostics`. Changes are validated 250ms after the last edit. |
| `textDocument/didClose` | Clears the document's diagnostics and forgets its context. |
| `workspace/didChangeConfiguration` | Applies settings, either as given or under a `jolt` property, and revalidates open documents. |
| `shutdown`, `exit` | Stop validating, then exit. |

Diagnostics use `Jolt` as their source and Jolt's documented error code (e.g. `JLT562`) as their code. Unsafe method usage is a warning, and everything else is an error.

### Settings

```json
{ "validation": { "allowUnsafeMethods": false } }
```

When `allowUnsafeMethods` is true, the use of unsafe methods (such as `#eval`) is not reported, for applications that enable them.

### Document context

Some issues depend on what the host application provides to a transformer, which the server can't know by itself. A client describes it with the `jolt/didChangeDocumentContext` notification, which it can send before or after opening the document, and again whenever it changes:

```json
{
  "textDocument": { "uri": "file:///transformers/order.jolt.json" },
  "methods": [ { "name": "reverseString" } ],
  "transformers": [ "address" ]
}
```

- `methods` lists the external methods that the host registers. Calls to any other non-library method are reported as unknown. External methods accept any arguments, since only their names are known.
- `transformers` lists the transformers that the host registers. `#transformWith()` calls with any other literal transformer name are reported.

Either list can be left out (or null), meaning that it isn't known, in which case the issues that depend on it are not reported. This is also the behavior for clients that never send the notification. An empty list means that the host provides none.

## Documents

A document is usually a single transformer. A Jolt test document (an object with a `testGroups` array, as used by `Tests/Jolt.Testing`) instead contains a transformer in each test's `transformer` property, and each of those is validated on its own.

## How issues are located

Jolt reports the path to the property or array element containing an issue (e.g. `$.items[0]['#foreach(@x in $.list)']`), and whether it's in the property's name or value. The server parses the document with its own position-aware JSON parser and builds the same paths, formatting each property segment with System.Text.Json (which Jolt uses), so they match exactly. Within the expression found that way, Jolt reports the span of each issue, which the server maps through any escape sequences in the JSON string:

- A syntax error covers the token where parsing failed, or the last character when the expression ended early (e.g. the `(` of an unclosed `#valueOf(`)
- An undeclared variable covers the variable (e.g. `@missing`)
- A method issue, including an unknown method, covers the method name (e.g. `#currentDateTime`)
- An unregistered transformer covers its quoted name (e.g. `'address'`)

For issues without a span (e.g. from a version of Jolt that doesn't report them), a syntax error covers the whole expression, and other issues are placed by searching the expression for the method, variable, or transformer they name, with several issues about the same name placed on successive occurrences of it.
