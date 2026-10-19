---
title: Error handling
description: Recovering from errors inside a transformer with #try.
sidebar:
  order: 12
---

By default, any error during transformation fails the entire transformation and throws an exception. To degrade gracefully when input is unexpected, catch errors inside the transformer with [`#try`](../../reference/library/try/).

```jolt
{
    "default": "#try(#valueOf($.stringValue)->#toInteger(), @e: #valueOf($.defaultValue))",
    "nullOnError": "#try(#valueOf($.stringValue)->#toInteger(), @e: null)"
}
```

`#try` takes two parameters:

1. the expression to evaluate, and
2. a lambda that receives the exception and returns the value to use when an error occurs.

Here, `default` tries to convert a string to an integer and falls back to `$.defaultValue` if that fails. `nullOnError` returns null on any error.

## Inspecting the error

The lambda's `@e` describes what went wrong:

| Property | Description |
| -------- | ----------- |
| `message` | A description of the error. |
| `exceptionType` | The .NET type of the error, such as `System.FormatException`. |
| `code` | The Jolt error code, such as `JLT563`, when Jolt raised the error. Null otherwise. |

`code` lets a fallback handle only the errors it expects. For example, it can treat a missing path (`JLT563`) or a null value (`JLT574`) differently from bad data:

```jolt
{
    "nickname": "#try(#toUpperCase($.nickname), @e: #if(@e.code == 'JLT563', 'no nickname', 'invalid nickname'))"
}
```

For values that are expected to be missing, the null-safe `?.` operator is usually simpler than `#try`. See [null handling](../null-handling/).

## Catching problems before runtime

Many errors are mistakes in the transformer rather than bad input. [`Validate()`](../../host/validation/) finds those without a source document.
