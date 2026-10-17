---
title: Null handling
description: The null-coalescing ?? operator and null-safe ?. dereferencing.
sidebar:
  order: 11
---

## Null-coalescing `??`

`??` returns its left operand when it isn't null, and its right operand otherwise.

```jolt
{
    "coalescingResult": "#valueOf($.some.nullableValue) ?? 'default value'"
}
```

## Null-safe dereferencing `?.`

`?.` in a path returns null when the value to its left is null, instead of throwing.

```jolt
{
    "@x": "#valueOf($.some)",
    "safeDereference": "@x.nullableObject?.property"
}
```

## Related methods

- [`#exists`](../../reference/library/exists/) is true when a value isn't null.
- [`#isNull`](../../reference/library/isnull/) is true when a value is null.
- [`#isMissing`](../../reference/library/ismissing/) is true when a path doesn't exist in the source document at all.
- [`#try`](../../reference/library/try/) recovers from errors. See [error handling](../error-handling/).
