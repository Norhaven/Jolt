`summarizeWith` performs aggregation over grouped data. It's easiest to understand starting from `#groupBy`.

Start with an array that could be grouped:

```json title="source.json"
{
  "someArray": [
    { "groupingProperty": "A", "valueProperty": 1 },
    { "groupingProperty": "A", "valueProperty": 2 },
    { "groupingProperty": "B", "valueProperty": 3 }
  ]
}
```

A plain grouping keeps all of the original data:

```jolt
{
  "groupedArray": "#groupBy($.someArray, @x: @x.groupingProperty)"
}
```

```json title="output.json"
{
  "groupedArray": [
    {
      "key": "A",
      "results": [
        { "groupingProperty": "A", "valueProperty": 1 },
        { "groupingProperty": "A", "valueProperty": 2 }
      ]
    },
    {
      "key": "B",
      "results": [
        { "groupingProperty": "B", "valueProperty": 3 }
      ]
    }
  ]
}
```

That's verbose. To get just the total of `valueProperty` per group, pipe the groups into `summarizeWith`. Its lambda receives each group as `@group`, with `@group.key` and `@group.results`:

```jolt
{
  "summarizedArray": "#valueOf($.someArray)->#groupBy(@x: @x.groupingProperty)->#summarizeWith(@group: { 'name': @group.key + ' total', 'totalValue': #sum(@group.results, @y: @y.valueProperty) })"
}
```

Each output element keeps the group's `key`, and the lambda's result becomes its `value`:

```json title="output.json"
{
  "summarizedArray": [
    { "key": "A", "value": { "name": "A total", "totalValue": 3 } },
    { "key": "B", "value": { "name": "B total", "totalValue": 3 } }
  ]
}
```

The summary doesn't have to be an object. `#summarizeWith(@group: #sum(@group.results))` on the same groups would produce `{ "key": "A", "value": 3 }` and `{ "key": "B", "value": 3 }`.

### Without `groupBy`

`summarizeWith` doesn't have to follow `#groupBy`. Any array of objects shaped like its output, with a `key` and a `results` array, works. `#groupBy` just does that work for you.
