---
title: Operators
description: Arithmetic, comparison, string concatenation and logical operators.
sidebar:
  order: 3
---

## Math and equality

Jolt implements addition, subtraction, multiplication, and division (`+`, `-`, `*`, `/`), along with comparison and equality (`==`, `!=`, `>`, `>=`, `<`, `<=`). Operations can be parenthesized, and method results can be operands.

```json title="source.json"
{
    "first": 5,
    "second": 4,
    "third": 3,
    "fourth": 2,
    "fifth": 1
}
```

```jolt title="transformer.json"
{
    "result": "#valueOf($.first) * #valueOf($.second) - #valueOf($.third) + #valueOf($.fourth) / #valueOf($.fifth) == 19"
}
```

```json title="output.json"
{
    "result": true
}
```

## String concatenation

`+` is overloaded to concatenate strings, whether they come from the source document or are literals.

```json title="source.json"
{
    "value": "example"
}
```

```jolt title="transformer.json"
{
    "result": "#valueOf($.value) + #valueOf($.value) + 'hello'"
}
```

```json title="output.json"
{
    "result": "exampleexamplehello"
}
```

## Logical operators

`&&` and `||` take boolean operands and return a boolean.

- `||` returns `true` if at least one operand is `true`, short-circuiting at the first `true`.
- `&&` returns `true` only if both operands are `true`, short-circuiting at the first `false`.

```json title="source.json"
{
    "first": true,
    "second": false,
    "third": true
}
```

```jolt title="transformer.json"
{
    "orResult": "#valueOf($.first) || #valueOf($.second) || #valueOf($.third)",
    "andResult": "#valueOf($.first) && #valueOf($.second) && #valueOf($.third)",
    "combinedResult": "(#valueOf($.first) && #valueOf($.second)) || #valueOf($.third)"
}
```

```json title="output.json"
{
    "orResult": true,
    "andResult": false,
    "combinedResult": true
}
```

`&&` binds tighter than `||`, so `combinedResult` would be `true` even without the parentheses.

### Logical NOT

`!` negates a boolean value.

```jolt
{
    "notFirst": "!#valueOf($.first)",
    "notSecond": "!#valueOf($.second)"
}
```

## Null-handling operators

`??` (null-coalescing) and `?.` (null-safe dereferencing) are covered in [null handling](../null-handling/).

## Precedence

See the [operator precedence table](../../reference/operator-precedence/) for how mixed expressions are grouped.
