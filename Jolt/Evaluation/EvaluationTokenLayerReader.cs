using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    internal sealed class EvaluationTokenLayerReader
    {
        private readonly Stack<Queue<EvaluationToken>> _layers = new Stack<Queue<EvaluationToken>>();

        public bool HasTokens => _layers.Count > 0;

        public void Enqueue(EvaluationToken token)
        {
            if (_layers.Count == 0)
            {
                _layers.Push(new Queue<EvaluationToken>());
            }

            _layers.Peek().Enqueue(token);
        }

        public void Push(EvaluationToken token)
        {
            var layer = new Queue<EvaluationToken>();

            layer.Enqueue(token);

            _layers.Push(layer);
        }

        public EvaluationToken? GetNextToken()
        {
            if (_layers.Count > 0)
            {
                var currentQueue = _layers.Peek();

                if (currentQueue.Count == 1)
                {
                    _layers.Pop();
                }

                return currentQueue.Dequeue();
            }

            return default;
        }
    }
}
