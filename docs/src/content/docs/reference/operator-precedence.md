---
title: Operator precedence
description: How Jolt groups binary and unary operators in mixed expressions.
---

Higher levels bind tighter. For example, `*` binds tighter than `+`, so multiplication happens first. Use parentheses to override.

| Operator | Description | Level |
| -------- | ----------- | ----- |
| `??` | Null coalescing | 0 |
| `!` | Logical NOT (unary) | 0 |
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

<!--
  TODO(language): Unary `!` is listed at level 0, below every binary operator. Read literally,
  `!a && b` would parse as `!(a && b)`. Most languages bind unary operators tighter than any
  binary operator. Confirm what the parser does and either fix the parser or this table.
  Also document associativity (left for all binary operators?) and where `->` and indexing sit.
-->

The full grammar is in the [EBNF reference](../grammar/).
