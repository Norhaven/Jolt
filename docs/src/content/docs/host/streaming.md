---
title: Streaming documents
description: Transforming NDJSON, RFC 7464 JSON sequences and other streams of documents.
sidebar:
  order: 5
---

`Transform(string)` assumes its input is a single document. For many documents at once, `JoltTransformer` (which the `JoltJsonTransformer` types inherit from) offers streaming methods.

## One document per line

For NDJSON / JSON Lines, where each line holds one complete object:

| Method | Description |
| ------ | ----------- |
| `TransformLines(TextReader reader, TextWriter writer, StreamingOptions? options)` | Reads one line at a time, transforms each, and writes the output. |
| `TransformLines(Stream input, Stream output, StreamingOptions? options)` | Same, over streams. |
| `TransformLinesAsync(TextReader reader, TextWriter writer, CancellationToken? cancellationToken, StreamingOptions? options)` | Asynchronous reading and writing. |
| `TransformLinesAsync(Stream input, Stream output, CancellationToken? cancellationToken, StreamingOptions? options)` | Asynchronous, over streams. |

## Other sequence formats

| Method | Description |
| ------ | ----------- |
| `TransformSequence(TextReader reader, TextWriter writer, StreamingOptions? options)` | Reads one JSON object at a time, transforms each, and writes the output. |
| `TransformSequence(Stream input, Stream output, StreamingOptions? options)` | Same, over streams. |
| `TransformSequenceAsync(TextReader reader, TextWriter writer, CancellationToken? cancellationToken, StreamingOptions? options)` | Asynchronous reading and writing. |
| `TransformSequenceAsync(Stream input, Stream output, CancellationToken? cancellationToken, StreamingOptions? options)` | Asynchronous, over streams. |

`TransformSequence` detects the input format automatically, trying these in order:

1. **RFC 7464 JSON text sequences**: objects separated by the `0x1E` record separator byte
2. **Whitespace-separated objects**: prettified or not, separated by newlines or other whitespace
3. **A JSON array of objects**

:::caution[Arrays load fully into memory]
The JSON array format reads the entire array into memory before processing. Prefer one of the first two formats for large inputs.
:::

## Output format

By default, output uses the same format and delimiter as the input. To control it, pass a `StreamingOptions`, which lets you choose how each output document is written (single line, multi-line, and others) and which delimiter to use (RFC 7464, newline, and so on).
