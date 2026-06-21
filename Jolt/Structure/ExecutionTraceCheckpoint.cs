using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents a checkpoint within a given execution trace operation.
    /// </summary>
    [DebuggerDisplay("{Name} - {TimeSinceLastCheckpoint}")]
    public sealed class ExecutionTraceCheckpoint
    {
        /// <summary>
        /// Gets the name of the checkpoint.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the time elapsed since the previous checkpoint occurred within this trace operation, if any.
        /// </summary>
        public TimeSpan TimeSinceLastCheckpoint { get; }

        /// <summary>
        /// Gets the time in ticks that has elapsed since this particular trace operation began.
        /// </summary>
        public long TicksSinceScopeOpened { get; }

        /// <summary>
        /// Gets the input values for this checkpoint (e.g. the transformer method arguments).
        /// </summary>
        public string[] InputValues { get; }

        /// <summary>
        /// Gets the output value for this checkpoint (e.g. the transformer method result). 
        /// </summary>
        public string? OutputValue { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ExecutionTraceCheckpoint"/> with the provided parameters.
        /// </summary>
        /// <param name="name">The name of this checkpoint.</param>
        /// <param name="timeSinceLastCheckpoint">The time that has elapsed since the previous checkpoint, if any.</param>
        /// <param name="ticksSinceScopeOpened">The time in ticks that has elapsed since this execution trace operation began.</param>
        /// <param name="inputValues">The input values for this checkpoint.</param>
        /// <param name="outputValue">The output value for this checkpoint.</param>
        internal ExecutionTraceCheckpoint(string name, TimeSpan timeSinceLastCheckpoint, long ticksSinceScopeOpened, string[] inputValues, string? outputValue)
        {
            Name = name;
            TimeSinceLastCheckpoint = timeSinceLastCheckpoint;
            TicksSinceScopeOpened = ticksSinceScopeOpened;
            InputValues = inputValues;
            OutputValue = outputValue;
        }
    }
}
