---
title: Methods and piping
description: Calling library and external methods, and chaining them with the -> operator.
sidebar:
  order: 2
---

The language relies mainly on methods to do transformation work, both those built into the library and [external methods](../../host/external-methods/) you write in C# and register.

## Calling methods

All method calls begin with `#` and are case-sensitive. Parentheses surround the arguments, which are comma-separated.

```jolt
{
    "value": "#valueOf($.some.json.path)"
}
```

The argument here is a JSON Path query. It selects the value at that path in the source document. Most library methods accept either a path, as above, or a value taken from the source document.

```jolt
{
    "valueExists": "#exists($.some.json.path)",
    "otherValueExists": "#exists(#valueOf($.some.other.json.path))"
}
```

## Piping

The `->` operator passes the result of one expression as the **first argument** of the next method. That argument is then left out of the second call. To skip the method (and the rest of the chain) when that result is null, use [`?->`](../null-handling/) instead.

```jolt
{
    "valueExists": "#valueOf($.some.json.path)->#exists()"
}
```

If you know C# extension methods, this is the same idea. Any method, including your own external methods, can be called standalone or piped. Piping lets a chain read left to right:

```jolt
{
    "names": "#valueOf($.people)->#where(@p: @p.active)->#select(@p: @p.name)->#joinWith(', ')"
}
```

You can also pipe from an array literal, an object literal (such as `{ 'country': 'US' }->#merge($.address)`) or a parenthesized expression (such as `(#valueOf($.price) * 1.2)->#roundTo(2)`), or directly from a variable, one of its properties, or an index or slice of it (such as `@names[1..]->#length()`):

```jolt
{
    "@someVariable": "#valueOf($.some.arrayPath)",
    "result": "@someVariable->#select(@y: @y.name->#length())"
}
```

## Built-in and external methods

- Every built-in method is documented in the [library reference](../../reference/library/).
- To call your own C#, see [external methods](../../host/external-methods/).
