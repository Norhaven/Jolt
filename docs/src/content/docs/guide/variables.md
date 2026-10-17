---
title: Range variables
description: Declaring variables, looping with #foreach, scoping rules, and iterating object properties.
sidebar:
  order: 5
---

Sometimes you want to run a transform and then do something more with the result, such as some math or a loop over it. Usually you don't want that intermediate result in the final output; it's only there for preprocessing. Range variables cover this.

All variables start with `@`. Writing one as a **property name** declares and sets it, and removes that node from the output.

## Declaring a variable from an expression

```jolt
{
    "@firstVar": "#valueOf($.someInteger)",
    "@secondVar": "#valueOf($.someOtherInteger)",
    "sum": "@firstVar + @secondVar"
}
```

## Declaring a variable from a loop

[`#foreach`](../../reference/library/foreach/) transforms each element of an array using a content template. Its result goes to a named property (`into 'name'`) or into a variable (`into @name`).

```jolt
{
    "#foreach(@x in $.someArray) into @tempResult": [
        {
            "intermediateValue": "@x.someInteger * 2"
        }
    ],
    "#foreach(@x;@i in @tempResult) into 'actualResult'": [
        {
            "finalValue": "@x.intermediateValue + 5",
            "currentIndex": "@i"
        }
    ]
}
```

The first loop stores the transformed array in `@tempResult` and removes the node from the output. The second loop iterates over that intermediate array, transforms it again, and names the final property `actualResult`.

To get the index as well as the value, declare two variables separated by a semicolon. The left one is the value and the right one is the index.

## Scope

Variables declared inside a loop's content template exist only within it, per iteration: they're redeclared, set, and discarded every time through the loop.

```jolt
{
    "#foreach(@x in $.someArray) into 'finalResult'": [
        {
            "@scopedVar": "@x.someInteger / 2",
            "subtractedValue": "@scopedVar - 3"
        }
    ],
    "invalidValue": "@scopedVar"
}
```

`finalResult` is populated by the loop, but `invalidValue` is null because `@scopedVar` no longer exists outside it.

:::caution[Shadowing]
A variable with the same name as an outer variable shadows it inside the inner scope. Prefer distinct names.
:::

## Looping over object properties

When `#foreach` iterates an object, the variable is set to each **property value** in turn.

```jolt
{
    "#foreach(@x in $.someObject) into 'result'": [
        {
            "propertyValue": "@x"
        }
    ]
}
```

A hardcoded property name like `propertyValue` is overwritten on every iteration. To keep each original property name, use [`#nameOf`](../../reference/library/nameof/) on the loop variable as the property name:

```jolt
{
    "#foreach(@x in $.someObject) into 'result'": [
        {
            "#nameOf(@x)": "@x"
        }
    ]
}
```

## Other uses of range variables

- [Lambdas](../lambdas/) bind variables scoped to the lambda body.
- [Indexing and slicing](../indexing-and-slicing/) works directly on variables.
- [`#using` blocks](../using-blocks/) bind a variable you modify with statements.
- [Pattern matching](../pattern-matching/) can bind variables inside patterns.
