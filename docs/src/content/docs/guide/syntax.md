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
| `'text'` | A string literal, which is always text, even when it looks like a path (e.g. `'$.a'`) | `'hello'` |
| `->` | [Pipe](../methods-and-piping/#piping) a value into a method's first parameter | `@x->#length()` |
| `?->` | [Pipe](../null-handling/) a value only when it isn't null, otherwise skip the rest of the chain | `@x.name?->#trim()` |
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

An expression that's an element of an array works the same way, and its result takes the element's place. Other elements stay where they are:

```jolt
{ "summary": [ "total", "#sum($.items, @x: @x.price)", "#length($.items)" ] }
```

With [loose error handling](../error-handling/), an element whose expression fails becomes null, so that the elements after it keep their positions.

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

## JSON paths

A path starts at `$` and selects a value from the source document. Everything inside square brackets is part of the path, so names and filters can contain spaces, commas and parentheses:

```jolt
{
    "simple": "#valueOf($.order.items[0].sku)",
    "bracketedName": "#valueOf($['some name'])",
    "filtered": "#valueOf($.order.items[?(@.sku == 'abc')].price)"
}
```

Outside of square brackets, a path ends at whitespace or at anything that can follow a value, such as `,`, `)`, `]`, `}` or an operator. That means `($.price*2)` and `[$.first,$.second]` work without spaces. Paths follow [RFC 9535](https://www.rfc-editor.org/rfc/rfc9535) with either JSON library, so a negative index counts from the end of an array, and a filter uses `match()` or `search()` for regular expressions. A name after a dot can only contain letters, digits and underscores, and can't start with a digit. Any other name needs bracket notation, such as `$['first-name']`, `$['1st']` or `$['a+b']`, and so does one within a filter: `$.items[?(@['unit-price'] > 1)]`.

A path with a name after a dot that needs brackets throws error `JLT586`, which says how to write it. That includes a subtraction without spaces, since `($.price-1)` is read as the name `price-1`, so write `($.price - 1)` instead. Any other path that can't be parsed throws error `JLT581`. Validating the transformer reports both.

### Paths that can match several values

A path with a wildcard, filter, slice, union or recursive descent can match several values, so it always gives an array, even if there's only one match or none:

| Path | Result |
| ---- | ------ |
| `$.items[0].sku` | The value, or null if it's missing |
| `$.items[*].sku` | An array of every match |
| `$.items[?(@.sku == 'abc')].price` | An array, with one element if one item matches |
| `$.items[?(@.price > 9000)]` | An empty array if nothing matches |

With no matches, [`#exists`](../../reference/library/exists/) gives false and [`#isMissing`](../../reference/library/ismissing/) gives true. [`#using`](../using-blocks/) needs an object, so it needs a path to one value, such as `$.items[0]`.

The path that [`#foreach`](../../reference/library/foreach/) loops over is searched for in the current loop element first, and then outward through any enclosing loops to the source document. For a path that can match several values, a source without any matches is passed over in the same way as a missing path. Any other path always starts from the source document.

## Paths and values

Most library methods accept either a JSON Path or a value. These are equivalent:

```jolt
{
    "valueExists": "#exists($.some.json.path)",
    "otherValueExists": "#exists(#valueOf($.some.json.path))"
}
```
