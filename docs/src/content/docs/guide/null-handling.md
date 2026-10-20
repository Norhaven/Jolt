---
title: Null handling
description: The null-coalescing ?? operator and null-safe ?. dereferencing.
sidebar:
  order: 11
---

## Null-coalescing `??`

`??` returns its left operand when it isn't null, and its right operand otherwise. The right operand is evaluated only when it's needed.

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

## Null values in library methods

Library methods that act on a value, such as [`#toUpperCase`](../../reference/library/touppercase/), [`#length`](../../reference/library/length/) or [`#select`](../../reference/library/select/), throw an error instead of guessing at a result when that value is missing or null:

- A path that doesn't exist in the source document, like `$.nickname` or `@x.nickname`, throws error `JLT563`, which names the missing path.
- A null value, such as a variable containing null or a path to a property whose value is null, throws error `JLT574`.

A misspelled path is reported where it's used, instead of quietly producing a null somewhere in the output.

When a value is optional, say so in the transformer. A null-safe `?.` dereference tells the method that a missing value is expected, and it returns a default instead of throwing. That default is null, `false` for methods that answer a true or false question (such as [`#startsWith`](../../reference/library/startswith/)), or an empty array for methods that return an array (such as [`#select`](../../reference/library/select/)). Combine it with `??` to choose a different default:

```jolt
{
    "@person": "#valueOf($.person)",
    "nickname": "#toUpperCase(@person?.nickname) ?? 'none'",
    "tagCount": "#length(@person.details?.tags) ?? 0"
}
```

`@person?.nickname` protects `nickname`, and `@person.details?.tags` protects `tags`. Each `?.` applies to the property right after it.

For a `$` path, put a default on the value before calling the method, or check for it first:

```jolt
{
    "nickname": "#toUpperCase(#valueOf($.nickname) ?? 'none')",
    "alias": "#if(#exists($.alias), #toUpperCase($.alias), 'none')"
}
```

[`#exists`](../../reference/library/exists/), [`#isMissing`](../../reference/library/ismissing/), [`#isNull`](../../reference/library/isnull/) and the type checks (`#isString`, `#isInteger`, `#isDecimal`, `#isBoolean`, `#isArray` and `#isObject`) never throw for missing or null values, because testing for them is what they're for. A condition in `#if` or `#when` that doesn't exist is treated as false.

To recover from these errors instead, see [error handling](../error-handling/).

## Related methods

- [`#exists`](../../reference/library/exists/) is true when a value isn't null.
- [`#isNull`](../../reference/library/isnull/) is true when a value is null.
- [`#isMissing`](../../reference/library/ismissing/) is true when a path doesn't exist in the source document at all.
- [`#try`](../../reference/library/try/) recovers from errors. See [error handling](../error-handling/).
