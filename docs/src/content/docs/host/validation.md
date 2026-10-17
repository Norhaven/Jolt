---
title: Validation
description: Checking a transformer for syntax and structural issues before running it.
sidebar:
  order: 4
---

Transformers can be hard to test without a real source document, which takes time to obtain and is specific to one case. `Validate()` on `JoltTransformer` gives you a quick check of syntax and expression shapes without one.

Create the `JoltJsonTransformer` with the `IJsonContext` holding the transformer you want to check, call `Validate()`, and iterate over any issues it returns.

Validation doesn't evaluate anything against a source document, so it won't catch runtime errors such as a variable holding the wrong kind of value. It finds mistakes in how the transformer is constructed, such as syntax misuse, and it verifies that every [`#transformWith`](../transformer-composition/) call resolves to a registered transformer.

## Issues

Each issue is a `Jolt.Structure.ValidationIssue`:

```csharp
public sealed class ValidationIssue
{
    public ValidationIssueType Type { get; }
    public ExceptionCode Code { get; }
    public string Message { get; }
    public string TransformerExpressionPath { get; }
    public string ExpressionText { get; }
    public bool IsInPropertyName { get; }
    public ExpressionSpan? Span { get; }
}
```

| Property | Meaning |
| -------- | ------- |
| `TransformerExpressionPath` | Path to the property or array element containing the expression. |
| `IsInPropertyName` | A property's name and value share one path, so this says whether the issue is in the **name** expression rather than the value. |
| `Span` | When known, the issue's location within the expression as `Start` and `Length`. |

What `Span` covers depends on the issue:

| Issue | Span covers |
| ----- | ----------- |
| Problem with a method call | The method name, e.g. `#valueOf` |
| Undeclared variable | The variable, e.g. `@x` |
| Unregistered transformer | The quoted transformer name |
| Syntax error | The token where parsing failed |
| Syntax error at the end of the expression, such as a missing `)` | An empty span at the end |

The [VS Code extension](../../getting-started/vscode-extension/) uses this same validation to show problems as you type.
