---
title: Your first transformer
description: Create a transformer, run it from C#, and see the output.
sidebar:
  order: 3
---

A Jolt transformation has three parts: a **source document** (the JSON you have), a **transformer** (JSON describing what you want), and the **output** Jolt produces.

## 1. Write a transformer

```jolt title="transformer.json"
{
    "name": "#valueOf($.customer.firstName) + ' ' + #valueOf($.customer.lastName)",
    "isActive": "#valueOf($.customer.status) == 'active'",
    "tagCount": "#valueOf($.customer.tags)->#length()"
}
```

Each string containing an expression is evaluated against the source document. `$.customer.firstName` is a JSON Path into the source.

## 2. Run it from C#

These examples use `Jolt.Json.Newtonsoft`; the `Jolt.Json.DotNet` API has the same shape.

```csharp
using Jolt.Json.Newtonsoft;

public string Run(string transformerJson, string sourceJson)
{
    var transformer = JoltJsonTransformer.DefaultWith<TransformerMethods>(transformerJson);
    return transformer.Transform(sourceJson);
}
```

`TransformerMethods` is the class holding any C# methods your transformer calls. See [external methods](../../host/external-methods/) for how to write and register them.

## 3. See the result

Given this source document:

```json title="source.json"
{
    "customer": {
        "firstName": "Ada",
        "lastName": "Lovelace",
        "status": "active",
        "tags": [ "math", "engines" ]
    }
}
```

The transformer produces:

```json title="output.json"
{
    "name": "Ada Lovelace",
    "isActive": true,
    "tagCount": 2
}
```

[Try this example in the playground →](../../playground/#code=dZBBCsIwEEWvMoxCDdUeQFAQt6IHMC6GOq2FNoFk0o307lKjtRbdhflvwrx_R2-DyxnXeNcGAEBjHrzYhp3GNbyGMSgq5-VIDfeJxt2VNC7HQE3j_GBbrinnCeSFJPiIUC5VOwWEymd8_gxj0JDcvtg4ZlNWhr3GT3CJz06bDpcojowvrGvYjT3NcOmspTrwqVjMs7d8NsgqSCGBBFL4hb2V1XCYxsrvXl7_Po8dKNhsIIkdJKN9oXJvg5H_-31FarWd1WxKuS1U795h9wA)

## Check your syntax early

Before you have a real source document, `Validate()` reports syntax and structure problems in the transformer itself. See [validation](../../host/validation/).

## Where to go next

- [Syntax](../../guide/syntax/) covers the rules every expression follows.
- [Methods and piping](../../guide/methods-and-piping/) explains `#method()` calls and the `->` operator.
- The [library reference](../../reference/library/) lists every built-in method.
