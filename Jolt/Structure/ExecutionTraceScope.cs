using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jolt.Structure
{
    public sealed class ExecutionTraceScope : IDisposable
    {
        public static ExecutionTraceScope CreateRootScopeWith(IMessageProvider messageProvider) => new ExecutionTraceScope(messageProvider, "Root");

        private readonly IMessageProvider _messageProvider;
        private readonly Stopwatch _scopeTimeTracker = new Stopwatch();
        private readonly Queue<ExecutionTraceCheckpoint> _checkpoints = new Queue<ExecutionTraceCheckpoint>();
        private readonly long _executionStartedAtTicks;
        private bool _isDisposed;

        public TimeSpan ScopeTimeElapsed => TimeSpan.FromTicks(_scopeTimeTracker.ElapsedTicks - _executionStartedAtTicks);
        public ExecutionTraceScope? ParentScope { get; private set; }
        public string TransformerName { get; }

        public ExecutionTraceScope(IMessageProvider messageProvider, string transformerName, ExecutionTraceScope? parentScope = default)
        {
            _messageProvider = messageProvider;
            TransformerName = transformerName;
            ParentScope = parentScope;

            if (ParentScope == null)
            {
                _scopeTimeTracker.Start();
                _executionStartedAtTicks = 0L;
            }
            else
            {
                _scopeTimeTracker = ParentScope._scopeTimeTracker;
                _executionStartedAtTicks = _scopeTimeTracker.ElapsedTicks;
            }
        }

        public ExecutionTraceScope OpenTransformerScopeFor(string transformerName)
        {
            return new ExecutionTraceScope(_messageProvider, transformerName ?? TransformerName, this);
        }

        public void WriteExpressionTextCheckpoint(string name, string expressionText)
        {
            WriteCheckpoint(name, expressionText, Array.Empty<string>(), default);
        }

        public void WriteInputCheckpoint(string name, params string[] inputValues)
        {
            WriteCheckpoint(name, default, inputValues ?? Array.Empty<string>(), default);
        }

        public void WriteOutputCheckpoint(string name, string outputValue)
        {
            WriteCheckpoint(name, default, Array.Empty<string>(), outputValue);
        }

        private void WriteCheckpoint(string name, string expressionText, string[] inputValues, string outputValue)
        {
            var currentTicks = _scopeTimeTracker.ElapsedTicks;
            var previousTicks = _checkpoints.Count > 0 ? _checkpoints.Peek().TicksSinceScopeOpened : 0L;

            var checkpoint = new ExecutionTraceCheckpoint(name, TimeSpan.FromTicks(previousTicks == 0 ? currentTicks : currentTicks - previousTicks), currentTicks, inputValues, outputValue);

            _checkpoints.Enqueue(checkpoint);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    var scopeClosedEntry = new ExecutionTraceEntry(TransformerName, _executionStartedAtTicks, ScopeTimeElapsed, _checkpoints, "Scope completed");

                    _messageProvider.WriteExecutionTraceFor(scopeClosedEntry);

                    _scopeTimeTracker.Stop();
                    _messageProvider.RemoveCurrentScope();
                }

                ParentScope = default;

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }
    }
}
