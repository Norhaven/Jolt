---
title: Execution tracing
description: Timing every method call in a transformation to find slow spots.
sidebar:
  order: 6
---

Call `WithExecutionTracing` on the `JoltOptions` you pass when creating the transformer to turn on performance measurement. Every method call is timed and checkpointed.

## Reading traces

After a run, traces are available on the `IMessageProvider` exposed by the `JoltContext`, through its `ExecutionTraces` property.

- `ExecutionTraces` contains **every** trace entry, wherever and whenever it occurred, for a complete overview.
- Each entry's `ChildEntries` gives a tree view of the scopes opened inside it.
- Entries are ordered by the start time of their scope, or of their root-level enclosing scope.

Each entry has its own scope, which records when it started, any checkpoints within it, and when it completed.

```csharp
public sealed class ExecutionTraceEntry
{
    public string TransformerName { get; }
    public DateTimeOffset Timestamp { get; }
    public long ExecutionStartedAtTicks { get; }
    public TimeSpan ExecutionTimeElapsed { get; }
    public ExecutionTraceCheckpoint[] Checkpoints { get; }
    public ExecutionTraceEntry[] ChildEntries { get; }
    public string Message { get; }
}

public sealed class ExecutionTraceCheckpoint
{
    public string Name { get; }
    public TimeSpan TimeSinceLastCheckpoint { get; }
    public long TicksSinceScopeOpened { get; }
    public string[] InputValues { get; }
    public string? OutputValue { get; }
}
```

## Formatted output

`ToString()` on an `ExecutionTraceEntry` formats it and all of its children. In this output from one of the tests, an outer scope calls into a partial transformer, and the child scope it opened is indented:

```txt
6/21/2026 5:47:04 PM +00:00 [Transformer: PartialTransformer] Scope completed
[Checkpoint: Invoking method 'Transform'] at 00:00:00.0004170
[Checkpoint: Method input '$.subPath,PartialTransformer,null,Jolt.Evaluation.EvaluationContext'] at 00:00:00.0001476
[Checkpoint: Method invocation completed with '{"sourceVariableX":null,"sourcePathValue":"test.source.path","integerValue":null,"textValue":null}'] at 00:00:04.1448826
  6/21/2026 5:46:59 PM +00:00 [Transformer: PartialTransformer] Scope completed
  [Checkpoint: Invoking method 'ValueOf'] at 00:00:00.0115538
  [Checkpoint: Method input '$.sourcePath,Jolt.Evaluation.EvaluationContext'] at 00:00:00.0000006
  [Checkpoint: Method invocation completed with 'test.source.path'] at 00:00:00.0006045
```
