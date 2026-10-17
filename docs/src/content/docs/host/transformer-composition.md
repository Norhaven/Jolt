---
title: Composing transformers
description: Splitting large transformers into named, reusable pieces called with #transformWith.
sidebar:
  order: 3
---

When a transformer grows too large to reason about in one file, split it into several and call them from each other with [`#transformWith`](../../reference/library/transformwith/). It takes a path to the sub-document to transform and the name of a registered transformer.

## Registering a transformer

You need a name and the transformer JSON:

```csharp
var registration = new TransformerRegistration("PartialTransformer", partialTransformerJson);
context.RegisterTransformer(registration);
```

## Calling it

In a property value of the primary transformer:

```jolt
{
    "part": "#transformWith($.some.path, 'PartialTransformer')"
}
```

This runs the registered transformer against the sub-document at `$.some.path` and uses its output as the value.

## Parameters

Variables from the calling transformer **aren't** visible in the called one. Instead, pass a JSON object as a third argument. It's stored in an implicit variable called `@params`.

```jolt title="primary transformer"
{
    "formattedDate": "#transformWith($.date, 'DateFormatter', { 'format': 'MM/dd/yyyy' })"
}
```

```jolt title="DateFormatter"
{
    "date": "#formatDateTime($.some.date, @params.format)"
}
```

When no parameters are passed, `@params` is an empty object rather than null. It's local to that transformer's execution.

## Patterns

### Enrich a partial result

Use [`#merge`](../../reference/library/merge/) to add context-specific properties to a focused, reusable transformer's output:

```jolt
{
    "enrichedResult": "#merge(#transformWith($.some.path, 'PartialTransformer'), { 'source': 'local', 'processedAt': #currentDateTimeUtc() })"
}
```

### Transform each element

```jolt
{
    "#foreach(@x in $.order.items) into 'lineItems'": [
        {
            "item": "#transformWith(@x, 'LineItemTransformer', { 'currency': #valueOf($.order.currency) })"
        }
    ]
}
```

## Validation

[`Validate()`](../validation/) checks that every `#transformWith` call refers to a registered transformer.
