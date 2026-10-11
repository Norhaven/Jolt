# IDE Roadmap: Server-Driven Language Features

This roadmap covers moving Jolt's editor support from the VS Code extension's own TypeScript and TextMate implementations to the Jolt language server, using an official token and analysis API from the Jolt library itself.

- **Near-term goal:** semantic highlighting from the language server. Today's TextMate grammar only gets the intended colors in the handful of themes the extension has configured. Semantic tokens use standard token types, which every theme colors.
- **Longer-term goal:** completions, hover, signature help, Go to Definition and rename served by the language server. These would then follow Jolt's own parsing rules instead of a separate TypeScript implementation that can drift from them.

Estimates assume someone familiar with the Jolt codebase.

| Phase | Area | Estimate |
|---|---|---|
| [1](#phase-1-jolt-library-official-analysis-api) | Jolt library: official analysis API | 5–9 days |
| [2](#phase-2-language-server-semantic-tokens) | Language server: semantic tokens | ~3 days |
| [3](#phase-3-vs-code-extension) | VS Code extension | ~2 days |
| **Total** | **Semantic highlighting** | **10–14 days** |
| [4](#phase-4-server-driven-language-features) | Server-driven completions and other features | 2–4 weeks more |

## What already exists

The diagnostics pipeline (find the expressions, get spans within them, map those spans into the document) is what semantic tokens need too. Most of the remaining work is in Jolt's public API.

| Capability | Where |
|---|---|
| A public tokenizer, where every token has a category and a span within its expression | [TokenReader.cs](../Jolt/Parsing/TokenReader.cs), [ExpressionToken.cs](../Jolt/Parsing/ExpressionToken.cs), [ExpressionTokenCategory.cs](../Jolt/Parsing/ExpressionTokenCategory.cs) |
| Parsed expression trees whose nodes carry spans | [Expression.cs](../Jolt/Expressions/Expression.cs) |
| A walk over the transformer that decides which strings are expressions, whether each is read as a property name or a value, and which variables are in scope | [JoltTransformerValidator.cs](../Jolt/JoltTransformerValidator.cs), [ValidationScope.cs](../Jolt/Structure/ValidationScope.cs) |
| A server-side JSON parser that maps each character of a string to its document offset, including escape sequences | `JsonStringSyntax.RawOffsets` in [JsonSyntax.cs](LanguageServer/Jolt.LanguageServer/Mapping/JsonSyntax.cs) |
| Finding the transformers inside test files, mapping JSON paths back to the document, and converting offsets to lines and characters | [DocumentValidator.cs](LanguageServer/Jolt.LanguageServer/Validation/DocumentValidator.cs), [JsonPathIndex.cs](LanguageServer/Jolt.LanguageServer/Mapping/JsonPathIndex.cs), [TextLines.cs](LanguageServer/Jolt.LanguageServer/Mapping/TextLines.cs) |

## Phase 1: Jolt library, official analysis API

**Estimate: 5–9 days.** This phase is the foundation for both semantic tokens and Phase 4.

- [ ] **Public transformer analysis API (2–4 days).**
  - Extract the validator's walk into a public API, e.g. `JoltTransformerAnalyzer`.
  - It returns one record per expression, with:
    - the expression's JSON path,
    - whether it's a property name or a value,
    - its tokens and parsed tree,
    - the variables it declares, with spans,
    - its validation issues.
  - `JoltTransformerValidator` becomes a consumer of the analyzer.
  - `ValidationScope` currently records variable names only, so it needs to record where each variable is declared.
- [ ] **Error-tolerant tokenizing (0.5–1 day).**
  - `ReadToEnd` throws at the first lexical error. Tokens before the error are still produced, because they're read lazily; the internal `TokenTracker` already relies on this.
  - Add a public method that returns the tokens read so far plus the failure point, so half-typed expressions are colored up to the error.
- [ ] **Exact token positions (about 1 day, recommended).**
  - Spans are currently worked out after each token is read by searching the text (`GetTokenSpan`). Some tokens take their value from the character after them, which that code has to work around.
  - Record each token's start inside the tokenizer instead, so every span is exact. Semantic tokens make any wrong span visible.
- [ ] **Classifying tokens by meaning (1–2 days).**
  - Token categories are lexical: a method name is just an `Identifier` after a `#`.
  - Add a public classification that distinguishes:
    - statements (`#foreach`, `#using`, `#match`, …) from value methods,
    - standard library, custom and unsafe methods,
    - a variable's declaration from a reference to it,
    - `@params`, keywords (`in`, `as`, `into`), type literals, operators and literals.
  - The validator's method resolver already knows most of this.
- [ ] **Splitting paths (0.5–1 day).**
  - `$.a.b[*]` is a single `PathLiteral` token, while the TextMate grammar colors `$`, property names, dots and wildcards separately.
  - Add a small path splitter so server-side coloring isn't a step backwards for paths.
- [ ] **Tests in `Jolt.Testing` (1–2 days).** Cover each token category and classification, spans in error cases, and declarations.

Design the analyzer with Phase 4 in mind. It should be able to answer cursor queries such as "the tokens and scope at this offset", so it doesn't need reworking later.

## Phase 2: Language server, semantic tokens

**Estimate: about 3 days.**

- [ ] **Protocol types (about 0.25 day).** In [LspTypes.cs](LanguageServer/Jolt.LanguageServer/Protocol/LspTypes.cs), add the semantic tokens capability, its list of token types and modifiers, and the request and response types.
- [ ] **The `textDocument/semanticTokens/full` handler (1–1.5 days).**
  - Parse the document, find each transformer, and run the analyzer.
  - Map spans through `RawOffsets` and `TextLines`, then sort the tokens and encode them as LSP's relative integers.
  - Share the parse with validation for each document version, rather than doing the work twice.
  - The delta and range requests can come later.
- [ ] **Refresh (about 0.25 day).** Send `workspace/semanticTokens/refresh` when the custom methods context changes, since that changes whether a method is classified as standard library or custom.
- [ ] **Tests in `Jolt.LanguageServer.Tests` (about 1 day).** Cover encoding, escape sequences, test files, multiple transformers, and expressions with errors.

## Phase 3: VS Code extension

**Estimate: about 2 days.**

- [ ] **Token types (about 0.5 day).**
  - Prefer standard types (`function`, `variable`, `parameter`, `keyword`, `operator`, `string`, `number`, `property`) with modifiers such as `declaration`, `defaultLibrary` and `readonly`. This is what fixes coloring in other themes.
  - Map any Jolt-specific types to the existing TextMate scopes via `semanticTokenScopes` in `package.json`, so the current color defaults keep working.
  - Consider turning `editor.semanticHighlighting.enabled` on by default for `[jolt]`.
- [ ] **TextMate fallback (about 0.5 day).**
  - Keep the TextMate grammar for when there's no server: before it starts, without .NET, or with the server turned off.
  - The client library picks up semantic tokens automatically once the server offers them, so no client code is needed.
- [ ] **Visual checks (about 1 day).** Check the configured themes and some others, test files, and expressions while they're being typed.

## Phase 4: Server-driven language features

**Estimate: 2–4 weeks more.** This builds on Phase 1.

- [ ] **Queries at the cursor on incomplete text.** Jolt's parser throws on unfinished input. Either make it tolerate errors, or use heuristics over the tokens around the cursor, as the TypeScript side does now.
- [ ] **Method metadata inside the server.** Move `LibraryMethodReader` from [Jolt.LanguageMetadata](Tools/Jolt.LanguageMetadata) into shared code (or into Jolt itself), so the server and the generated `library-methods.json` describe methods the same way.
- [ ] **Full custom method metadata.** The server currently only receives custom method names (`jolt/didChangeDocumentContext`). It needs the full definitions, or it could read `.jolt/methods.json` files itself.
- [ ] **LSP handlers.** Completion, hover, signature help, definition and rename.
- [ ] **Retiring the TypeScript implementations.** Either keep them as the fallback for users without .NET, or accept that these features require .NET (see the open decisions).

## Open decisions

- **Server lifecycle.** The server only runs while `jolt.validation.enabled` is on. Either highlighting (and later completions) follow that setting, or it splits into separate validation and server-features settings.
- **Without .NET.** Decide whether the TypeScript features and TextMate grammar stay as a supported fallback for users without .NET, or are removed once the server covers them.
- **Two highlighters.** The TextMate grammar and the server would both need updating whenever the syntax changes, unless the grammar is reduced to a minimal fallback after Phase 3.

## Risks

- **Repainting on startup.** TextMate colors show first, and semantic colors replace them once the .NET server starts. That's normal for LSP, but noticeable.
- **Coloring while typing.** Lexical errors cut off coloring from the error onwards. Parse errors keep all the tokens, but lose the classifications that need the parsed tree, such as marking declarations.
- **Span accuracy.** If Phase 1's exact token positions are skipped, a wrong span shows up as a wrongly colored character, and some categories have edge cases. Tests for each category reduce this risk.
