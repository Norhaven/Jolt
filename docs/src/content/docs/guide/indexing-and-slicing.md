---
title: Indexing and slicing
description: Using range expressions to take elements and sub-ranges of strings and arrays.
sidebar:
  order: 7
---

A range expression in square brackets indexes into a string or array at a specific point or range. It's shorthand for the [`#substring`](../../reference/library/substring/) and [`#slice`](../../reference/library/slice/) library methods.

```json title="source.json"
{
    "text": "this is an example",
    "numbers": [ 1, 9, 3, 2, 6 ]
}
```

```jolt title="transformer.json"
{
    "@someText": "#valueOf($.text)",
    "@someNumbers": "#valueOf($.numbers)",
    "textStart": "@someText[..2]",
    "textMiddle": "@someText[1..3]",
    "textEnd": "@someText[^2..]",
    "textCharacter": "@someText[^1]",
    "arrayStart": "@someNumbers[..3]",
    "arrayMiddle": "@someNumbers[2..4]",
    "arrayEnd": "@someNumbers[^3..]",
    "arrayElement": "@someNumbers[1]"
}
```

```json title="output.json"
{
    "textStart": "th",
    "textMiddle": "hi",
    "textEnd": "le",
    "textCharacter": "e",
    "arrayStart": [ 1, 9, 3 ],
    "arrayMiddle": [ 3, 2 ],
    "arrayEnd": [ 3, 2, 6 ],
    "arrayElement": 9
}
```

## How ranges work

Range expressions behave like C# ranges:

- An integer literal, range variable, or method result is an offset from the **start**.
- `^n` is an offset from the **end**.
- `a..b` is a half-open range: it includes `a` and excludes `b`. Either side can be omitted.
- A single index (`[1]`, `[^1]`) returns one element or character rather than a sub-range.

## Ranges as method arguments

Library methods that take a range, such as `#substring` and `#slice`, accept the range directly without brackets:

```jolt
{
    "middle": "#substring($.some.text, 2..5)",
    "lastThree": "#slice($.some.array, ^3..)"
}
```

## Indexing a method result

You don't need a variable. You can index a method's string or array result directly:

```jolt
{
    "firstTwo": "#valueOf($.someString)[..2]"
}
```
