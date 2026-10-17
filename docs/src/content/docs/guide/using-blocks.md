---
title: Using blocks and statements
description: Modifying a copy of a document in place with #using, #setAt, #removeAt and #when.
sidebar:
  order: 8
---

Sometimes you don't know the full shape of the incoming document, but you know it should stay the same apart from a few changes. Or you want to copy parts of the source into the output with some tweaks to their data or structure. In both cases you can't write a transformer that spells out every node ahead of time.

A [`#using`](../../reference/library/using/) block binds a path or variable to a range variable, then runs **statements** that modify it.

## Example

```json title="source.json"
{
    "some": {
        "integerArray": [ 1, 2, 6, 10 ],
        "complexObject": {
            "some": {
                "path": 5
            }
        }
    }
}
```

This transformer takes the `some` node, removes `integerArray`, adds a property to `complexObject`, and changes the value of `path`:

```jolt title="transformer.json"
{
    "#using($.some as @x) into 'result'": [
        "#removeAt(@x.integerArray)",
        "#setAt(@x.complexObject.new.value, 3)",
        "#setAt(@x.complexObject.some.path, 20)"
    ]
}
```

```json title="output.json"
{
    "result": {
        "complexObject": {
            "new": {
                "value": 3
            },
            "some": {
                "path": 20
            }
        }
    }
}
```

Statements are allowed only inside a `#using` block, and they must operate on the block's range variable.

[`#setAt`](../../reference/library/setat/) creates any missing parts of the hierarchy, which is why `@x.complexObject.new.value` works even though `new` didn't exist.

## Conditional statements

[`#when`](../../reference/library/when/) takes a boolean expression and one or more statements to run when it's true.

```jolt
{
    "#using($.some as @x) into 'result'": [
        "#when(#exists(@x.integerArray), #removeAt(@x.integerArray), #removeAt(@x.otherComplexObject))",
        "#when(#valueOf(@x.complexObject.some.path) > 3, #setAt(@x.complexObject.some.other.path, 20))"
    ]
}
```

## Storing the result in a variable

As with `#foreach`, you can send a `#using` block's output into a variable instead of a named property, then use it in a later block or anywhere else a variable is valid.

```jolt
{
    "#using($.some as @x) into @tempResult": [
        "#removeAt(@x.integerArray)"
    ],
    "#using(@tempResult as @x) into 'actualResult'": [
        "#setAt(@x.some.path, 'text')"
    ]
}
```

## Values from outside the block

`#setAt` values can come from an outer-scoped variable or a non-statement method call, the same as a `#when` condition can.

```jolt
{
    "@newValue": "#valueOf($.some.newValue)",
    "#using($.some as @x) into 'result'": [
        "#removeAt(@x.integerArray)",
        "#setAt(@x.complexObject.new.value, @newValue)",
        "#setAt(@x.complexObject.some.path, #valueOf($.someOtherValue))"
    ]
}
```
