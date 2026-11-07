---
title: Null handling
description: The null-coalescing ?? operator, null-safe ?. dereferencing and null-conditional ?-> piping.
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

## Dereferencing missing and null properties

Dereferencing a property that doesn't exist gives null. Dereferencing a property of a value that is null or missing throws error `JLT530`, since there's nothing to look for the property in:

```jolt
{
    "@person": "#valueOf($.person)",
    "nickname": "@person.nickname",
    "nicknameLength": "@person.nickname.length"
}
```

When `person` has no `nickname`, `nickname` is null, while `nicknameLength` throws, because it looks for `length` in a value that doesn't exist.

## Null-safe dereferencing `?.`

`?.` works the same way as C#'s `?.` operator. When the value to its left is null or missing, it gives null instead of throwing, and skips the rest of the chain, including any methods piped from it:

```jolt
{
    "@x": "#valueOf($.some)",
    "safeDereference": "@x.nullableObject?.property.name"
}
```

When `nullableObject` is null or missing, `safeDereference` is null, and neither `property` nor `name` is looked for. Each `?.` guards only the value to its left, so `@x?.nullableObject.property` still throws when `nullableObject` is null. Use `?.` at each step that can be null.

## Null-conditional piping `?->`

`?->` pipes a value into a method only when that value isn't null or missing. Otherwise, the method isn't called, and neither is any method piped after it in the same chain. The whole chain gives null instead:

```jolt
{
    "@person": "#valueOf($.person)",
    "nickname": "@person.nickname?->#trim()->#toUpperCase() ?? 'none'",
    "firstTag": "#valueOf($.tags)?->#take(1)"
}
```

When `nickname` is null or missing, neither `#trim` nor `#toUpperCase` is called, and `?? 'none'` supplies the default. `?->` works after a variable, a dereferenced property, a method call or an indexer.

A null-safe `?.` dereference does the same for a chain piped from it: in `@person?.nickname->#trim()`, `#trim` isn't called when `@person` is null or missing. When `@person` is present, `nickname` is piped into `#trim` even when it's null or missing, so use `@person?.nickname?->#trim()` to skip it then too. For both operators, null counts the same as missing. To pass a null value to the method, use `->` instead.

Skipping stops at the end of the chain. When a chain is another method's argument, that method is still called, and it receives null.

## Null values in library methods

Library methods that act on a value, such as [`#toUpperCase`](../../reference/library/touppercase/), [`#length`](../../reference/library/length/) or [`#select`](../../reference/library/select/), throw an error instead of guessing at a result when that value is missing or null:

- A JSON path that doesn't exist in the source document, like `$.nickname`, throws error `JLT563`, which names the missing path.
- A null value throws error `JLT574`. That includes a variable containing null, a property whose value is null or that doesn't exist (like `@x.nickname`), and a chain skipped by a null-safe `?.` dereference.

A misspelled path is reported where it's used, instead of quietly producing a null somewhere in the output.

When a value is optional, say so in the transformer. Skip the method with `?->`, or give it a default with `??`:

```jolt
{
    "@person": "#valueOf($.person)",
    "nickname": "@person?.nickname?->#toUpperCase() ?? 'none'",
    "tagCount": "#length(@person.details?.tags ?? [])"
}
```

`?->` skips `#toUpperCase` when `nickname` is null or missing, and the `?.` before it skips the chain when `@person` is. In `tagCount`, `?.` gives null when `details` is null or missing, and `??` replaces it, along with a null or missing `tags`, with an empty array.

For a `$` path, put a default on the value before calling the method, or check for it first:

```jolt
{
    "nickname": "#toUpperCase(#valueOf($.nickname) ?? 'none')",
    "alias": "#if(#exists($.alias), #toUpperCase($.alias), 'none')"
}
```

[`#exists`](../../reference/library/exists/), [`#isMissing`](../../reference/library/ismissing/), [`#isNull`](../../reference/library/isnull/) and the type checks (`#isString`, `#isInteger`, `#isDecimal`, `#isBoolean`, `#isArray` and `#isObject`) never throw for missing or null values, because testing for them is what they're for. A condition in `#if` that doesn't exist is treated as false, while a null condition in [`#when`](../../reference/library/when/) throws error `JLT580`.

To recover from these errors instead, see [error handling](../error-handling/).

## Related methods

- [`#exists`](../../reference/library/exists/) is true when a value isn't null.
- [`#isNull`](../../reference/library/isnull/) is true when a value is null.
- [`#isMissing`](../../reference/library/ismissing/) is true when a path doesn't exist in the source document at all.
- [`#try`](../../reference/library/try/) recovers from errors. See [error handling](../error-handling/).
