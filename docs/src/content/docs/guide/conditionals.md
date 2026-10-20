---
title: Conditional logic
description: Choosing values with #if and including whole objects with #includeIf.
sidebar:
  order: 4
---

Jolt gives you several ways to make decisions in a transformer. For choosing between many cases based on the shape of the data, see [pattern matching](../pattern-matching/).

## `#if`

[`#if`](../../reference/library/if/) takes three parameters: a boolean condition, an expression to evaluate when the condition is true, and an expression to evaluate when it's false.

```jolt
{
    "result": "#if(#valueOf($.someValue) > 5, 'Greater than 5', 'Less than or equal to 5')"
}
```

`result` is `"Greater than 5"` when `$.someValue` is greater than 5, and `"Less than or equal to 5"` otherwise.

## `#includeIf`

[`#includeIf`](../../reference/library/includeif/) sits in a property name. It takes a path or boolean condition, and when it's true it evaluates and includes the property's object. Otherwise the property is null.

```jolt
{
    "#includeIf(#valueOf($.someValue) > 5) into 'result'": {
        "message": "Greater than 5"
    }
}
```

This includes `result` with its nested `message` when `$.someValue` is greater than 5, and sets `result` to null otherwise.
