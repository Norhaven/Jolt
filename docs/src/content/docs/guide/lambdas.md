---
title: Lambdas
description: Passing inline functions to library and external methods.
sidebar:
  order: 6
---

Several library methods take a **lambda** to filter, project, or otherwise refine data. A lambda declares one or more range variables, separated by semicolons, then a colon and a body expression:

```jolt-expr
@x: @x.price > 10
```

```jolt-expr
@acc;@current: @acc + @current
```

A lambda's variables exist only inside its body.

## Example

```jolt title="transformer.json"
{
    "hasValues": "#valueOf($.some.integerArray)->#any(@x: @x > 5)",
    "filteredData": "#valueOf($.some.complexArray)->#where(@x: @x.some.path < 10)",
    "projectedData": "#valueOf($.some.complexArray)->#select(@x: @x.some.path)"
}
```

```json title="source.json"
{
    "some": {
        "integerArray": [ 1, 2, 6, 10 ],
        "complexArray": [
            { "some": { "path": 5 } },
            { "some": { "path": 30 } }
        ]
    }
}
```

```json title="output.json"
{
    "hasValues": true,
    "filteredData": [
        { "some": { "path": 5 } }
    ],
    "projectedData": [ 5, 30 ]
}
```

## Methods that take lambdas

| Arity | Methods |
| ----- | ------- |
| One parameter | [`any`](../../reference/library/any/), [`where`](../../reference/library/where/), [`select`](../../reference/library/select/), [`orderBy`](../../reference/library/orderby/), [`orderByDesc`](../../reference/library/orderbydesc/), [`groupBy`](../../reference/library/groupby/), [`summarizeWith`](../../reference/library/summarizewith/), [`distinct`](../../reference/library/distinct/), [`takeWhile`](../../reference/library/takewhile/), [`skipWhile`](../../reference/library/skipwhile/), [`max`](../../reference/library/max/), [`min`](../../reference/library/min/), [`sum`](../../reference/library/sum/), [`average`](../../reference/library/average/), [`try`](../../reference/library/try/) |
| Two parameters | [`reduce`](../../reference/library/reduce/), [`zip`](../../reference/library/zip/) |

Your own C# methods can take lambdas too. See [lambdas in external methods](../../host/external-methods/#lambdas-as-parameters).
