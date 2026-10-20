---
title: Syntax
description: The rules every Jolt expression follows, and where expressions can appear in a transformer.
sidebar:
  order: 1
---

A transformer is ordinary JSON. Jolt looks for **expressions** inside its property names and string values, evaluates them against the source document, and writes the results to the output.

## The building blocks

| Token | Meaning | Example |
| ----- | ------- | ------- |
| `#name(...)` | A [method call](../methods-and-piping/). Names are case-sensitive. | `#valueOf($.a)` |
| `$...` | A JSON Path into the source document | `$.order.items` |
| `@name` | A [range variable](../variables/) | `@total` |
| `'text'` | A string literal | `'hello'` |
| `->` | [Pipe](../methods-and-piping/#piping) a value into a method's first parameter | `@x->#length()` |
| `@x: ...` | A [lambda](../lambdas/) | `@x: @x.price > 10` |
| `a..b`, `^n` | A [range](../indexing-and-slicing/) | `@text[1..3]` |
| `into` | Names the output of a property-name construct | `#foreach(@x in $.a) into 'b'` |

Operators (`+ - * / == != < > <= >= && || ! ??`) are covered in [operators](../operators/).

## One expression per string

Each property name or value that contains an expression must hold exactly one complete expression. All of these are parse errors:

| Expression | Problem |
| ---------- | ------- |
| `#valueOf($.a))` | Leftover closing parenthesis |
| `#valueOf($.a) @b` | Two expressions with no operator between them |
| `#valueOf($.a) ==` | An operator with nothing after it |

## Where expressions appear

Where an expression sits decides what it can do.

**Property values** produce the output value for that property. Most library methods are used here.

```jolt
{ "total": "#sum($.items, @x: @x.price)" }
```

**Property names** declare variables or introduce constructs that shape the output, such as `#foreach`, `#using`, `#match`, and `#includeIf`. The `into` keyword names the resulting property, or stores it in a variable.

```jolt
{
    "@discount": "#valueOf($.discount)",
    "#foreach(@x in $.items) into 'lines'": [ { "sku": "@x.sku" } ]
}
```

**Statements** are array elements inside a [`#using` block](../using-blocks/) that modify a document in place: `#setAt`, `#removeAt`, and `#when`.

**Match cases** are property names inside a [`#match`](../pattern-matching/) block: `#is`, `#given`, and `#default`.

Each method's reference page lists where it's valid under **Valid in**.

The arguments of a method in a property name are values, so any method you call inside them only needs to be valid in a property value:

```jolt
{
    "#includeIf(#exists($.discount)) into 'discount'": {
        "amount": "#valueOf($.discount)"
    }
}
```

## Paths and values

Most library methods accept either a JSON Path or a value. These are equivalent:

```jolt
{
    "valueExists": "#exists($.some.json.path)",
    "otherValueExists": "#exists(#valueOf($.some.json.path))"
}
```
