# Change Log

All notable changes to the Jolt Language extension will be documented in this file.

### 1.4.0

- Signature help for library and custom method arguments, also shown by `Ctrl+Space` alongside completions
- Variable hover showing where a variable is declared
- Go to Definition for variables and custom methods
- Rename, Find All References, and occurrence highlighting for variables
- Snippets for `#foreach`, `#using`, `#match`, and variable declarations, offered where a property would go
- Likely misspellings of standard library methods (e.g. `#valeuOf`) are reported as warnings, with a quick fix, even without a `.jolt/methods.json` file
- Quick fixes for unknown methods (similar names, or adding the method to a `.jolt/methods.json` file) and undeclared variables (similar names, or declaring the variable)
- Method hover and Go to Definition no longer match method names within string literals
- JSON escape sequences within string literals (e.g. `'it\\'s'`) are now read as Jolt reads them by completions, signature help, and variable scoping
- Document formatting and range formatting
- Outline and breadcrumbs for transformers
- Language status items for validation, the applicable `.jolt/methods.json` files, and the included Jolt version
- The included Jolt version is shown in standard library method hover documentation
- Standard library metadata is regenerated from Jolt when the extension is packaged (updated to Jolt 2.4.3)
- Restart Language Server and Show Language Server Output commands
- Show Output, Retry, and Disable Validation actions when the language server can't be started
- Supports VS Code 1.101 and later (previously 1.138), so that editors based on older versions of VS Code can install it from Open VSX

### 1.3.0

Larger release for method metadata and a Jolt Language Server. This includes:
- Optional custom method return types (in `.jolt/methods.json` files)
- On-Hover documentation for C# return types
- On-Hover documentation for library and custom method lambda parameters 
- Syntax validation using the Jolt library within the Jolt Language Server

### 1.2.0

Targeted release for custom method support. This includes:
- Custom method definition files (i.e. `.jolt/methods.json`)
- Code completion support for custom methods in these files
- On-Hover documentation for custom methods

### 1.1.0

Followup release of the Jolt language extension. This includes:
- Method completions
- Variable completions
- On-Hover documentation

### 1.0.0

Initial release of the Jolt language extension. This includes:
- Language syntax colorization within `*.jolt.json` files.