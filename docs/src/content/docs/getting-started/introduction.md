---
title: Introduction
description: What Jolt is, what it's for, and when to reach for it instead of plain C#.
sidebar:
  order: 1
---

Jolt is an imperative JSON transformation language inspired by XSLT and the .NET JSON adaptation of it, [JUST.Net](https://github.com/WorkMaze/JUST.net). This project provides an expression-based interpreter for the language and a highly extensible way of approaching one problem: how to transform JSON into different JSON.

:::note[Looking for a different Jolt?]
The much older and unrelated .NET port of the Java JSON transformation library (by coincidence, also called Jolt) is [over here](https://github.com/blushingpenguin/Jolt.Net).
:::

## What could I use it for?

Say you have a third-party JSON-based REST API you need to call. Your data may already be in JSON, for instance in a NoSQL database, but its structure doesn't match what the API endpoint expects. You need to transform your JSON into different JSON, and that's the ideal use case for Jolt. You write a transformer in JSON that takes your data as input, calls into any transformation business logic in your code, and gives you the third-party API contract JSON as a result.

Any time you have JSON on hand that needs to be modified, whether that means adjusting values and structure slightly or creating something completely different, that's an opportunity for a Jolt transformer.

## What stops me from doing this myself in C#?

Absolutely nothing. If your changes are very minor, it may well be easier and better to do that than to maintain transformer artifacts.

As your transformation needs grow, though, hand-written tree-walking code tends to cause headaches. The largest pain points with restructuring JSON are ensuring that:

1. the values and structure are readable, maintainable, and reusable over time, and
2. your business logic isn't mixed in with the restructuring logic.

It's easy, especially with AI in the picture, to throw together code that walks the JSON tree and builds a new one. It's much harder to live with the aftermath and make sure you and other developers can understand and work with it.

## A first look

A transformer is a JSON document whose property names and values can contain Jolt expressions:

```jolt title="transformer.json"
{
    "value": "#valueOf($.some.json.path)",
    "shouted": "#valueOf($.some.json.path)->#toUpperCase()"
}
```

Continue with [installation](../installation/) and [your first transformer](../first-transformer/), or jump into the [language guide](../../guide/syntax/).
