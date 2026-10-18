---
title: Operator precedence
description: How Jolt groups binary and unary operators in mixed expressions.
---

Higher levels bind tighter. For example, `*` binds tighter than `+`, so multiplication happens first. Use parentheses to override.

| Operator | Description | Level |
| -------- | ----------- | ----- |
| `??` | Null coalescing | 0 |
| `\|\|` | Logical OR | 1 |
| `&&` | Logical AND | 2 |
| `==` | Equals | 3 |
| `!=` | Not equals | 3 |
| `<` | Less than | 4 |
| `>` | Greater than | 4 |
| `<=` | Less than or equal | 4 |
| `>=` | Greater than or equal | 4 |
| `+` | Addition, string concatenation | 5 |
| `-` | Subtraction | 5 |
| `*` | Multiplication | 6 |
| `/` | Division | 6 |
| `!` | Logical NOT (unary) | 7 |

Unary `!` binds tighter than any binary operator, so it negates only the operand right after it: `!a && b` is `(!a) && b`. To negate a larger expression, parenthesize it: `!(a && b)`.

<!--
  TODO(language): Document associativity (left for all binary operators?) and where `->` and indexing sit.
-->

The full grammar is in the [EBNF reference](../grammar/).
