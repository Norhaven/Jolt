using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a runtime trace that contains information about the executed methods.
    /// </summary>
    public sealed class ExecutionTraceEntry
    {
        /// <summary>
        /// Gets the name of the transformer that the current trace entry was in. If at the top level, the transformer will be named "Root".
        /// </summary>
        public string TransformerName { get; }

        /// <summary>
        /// The timestamp in UTC that the entry was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the amount of ticks that this execution trace began at. This will be relative to any parent entries, or zero if no parent entries exist.
        /// </summary>
        public long ExecutionStartedAtTicks { get; }

        /// <summary>
        /// Gets the total time elapsed during the execution of this particular trace.
        /// </summary>
        public TimeSpan ExecutionTimeElapsed { get; }

        /// <summary>
        /// Gets the checkpoints that occurred during this particular trace. Checkpoints will break down a trace into separate operations.
        /// </summary>
        public ExecutionTraceCheckpoint[] Checkpoints { get; } = Array.Empty<ExecutionTraceCheckpoint>();

        /// <summary>
        /// Gets the child trace entries that occurred and are nested within this particular trace.
        /// </summary>
        public ExecutionTraceEntry[] ChildEntries { get; }

        /// <summary>
        /// The message associated with this particular trace operation.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Initializes a new instance of <see cref="ExecutionTraceEntry"/> with the provided parameters.
        /// </summary>
        /// <param name="transformerName">The name of the transformer the trace occurred within.</param>
        /// <param name="executionStartedAtTicks">The number of ticks that this trace started at, relative to its parent (if any).</param>
        /// <param name="executionTimeElapsed">The total execution time elapsed during this trace.</param>
        /// <param name="checkpoints">The checkpoints that occurred within this trace.</param>
        /// <param name="message">The message associated with this trace.</param>
        /// <param name="executionTraceEntries">The sub-entries that are nested under this entry.</param>
        internal ExecutionTraceEntry(string transformerName, long executionStartedAtTicks, TimeSpan executionTimeElapsed, IEnumerable<ExecutionTraceCheckpoint> checkpoints, string message, IEnumerable<ExecutionTraceEntry>? executionTraceEntries = default)
        {
            TransformerName = transformerName;
            ExecutionStartedAtTicks = executionStartedAtTicks;
            ExecutionTimeElapsed = executionTimeElapsed;
            Checkpoints = checkpoints.ToArray();
            ChildEntries = executionTraceEntries?.ToArray() ?? Array.Empty<ExecutionTraceEntry>();
            Message = message;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        private string ToString(int indentCount)
        {
            const int PaddingPerIndent = 2;

            var indent = new string(Enumerable.Repeat(' ', PaddingPerIndent * indentCount).ToArray());

            var builder = new StringBuilder();

            builder.AppendLine($"{indent}{Timestamp} [Transformer: {TransformerName}] {Message}");

            foreach(var checkpoint in Checkpoints)
            {
                builder.AppendLine($"{indent}[Checkpoint: {checkpoint.Name}] at {checkpoint.TimeSinceLastCheckpoint}");
            }

            foreach(var entry in ChildEntries)
            {
                var entryString = entry.ToString(indentCount + 1);
                builder.AppendLine(entryString);
            }

            return builder.ToString();
        }
    }
}
