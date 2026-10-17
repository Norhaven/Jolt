---
title: Array and object literals
description: Building arrays and objects inline inside an expression.
sidebar:
  order: 10
---

You can build arrays and objects directly in an expression, without depending on the shape of the source document. Literals work anywhere a property-value expression does.

## Arrays

Square brackets enclose array elements. Elements can be literals (including nested arrays and objects), range variables, or method results.

```jolt
{
    "@x": "#valueOf($.some.path)",
    "array": "[ 1, 'two', null, @x, #valueOf($.some.path) ]"
}
```

## Objects

Curly braces enclose object properties. Property **names** must be string literals. Property **values** can be literals (including nested arrays and objects), range variables, or method results.

```jolt
{
    "@x": "#valueOf($.some.path)",
    "object": "{ 'integerValue': 5, 'stringValue': 'test', 'booleanValue': true, 'rangeVariable': @x, 'methodCallResult': #valueOf($.some.path) }",
    "nestedObject": "{ 'nested': { 'array': [ 1, 2, 3 ], 'object': { 'key': @x } } }"
}
```

Object literals are handy for passing parameters to [`#transformWith`](../../host/transformer-composition/) and for enriching results with [`#merge`](../../reference/library/merge/).
