# Change Log

All notable changes to the Jolt Language extension will be documented in this file.

### Unreleased

- Signature help for library and custom method arguments, also shown by `Ctrl+Space` alongside completions
- Variable hover showing where a variable is declared
- Go to Definition for variables and custom methods
- Rename for variables
- Document formatting and range formatting
- Outline and breadcrumbs for transformers
- Language status items for validation and the applicable `.jolt/methods.json` files
- Restart Language Server and Show Language Server Output commands
- Show Output, Retry, and Disable Validation actions when the language server can't be started

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