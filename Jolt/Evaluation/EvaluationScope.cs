using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Evaluation
{
    internal sealed class EvaluationScope : IEvaluationScope
    {
        public static EvaluationScope Empty => new EvaluationScope(new Stack<IJsonToken>(), new Stack<IList<RangeVariable>>());

        private readonly Stack<IJsonToken> _closures;
        private readonly Stack<IList<RangeVariable>> _variables;

        public IEnumerable<IJsonToken> AvailableClosures => _closures.Reverse();
        public int VariableCount => _variables.Count == 0 ? 0 : _variables.Peek().Count;

        public EvaluationScope(Stack<IJsonToken> closures, Stack<IList<RangeVariable>> variables)
        {
            _closures = closures;
            _variables = variables;
        }

        public bool TryGetVariable(string variableName, out RangeVariable? variable)
        {
            variable = default;

            if (_variables.Count == 0)
            {
                return false;
            }

            var currentLayers = new Stack<IList<RangeVariable>>(_variables);

            while (currentLayers.TryPop(out var current))
            {
                variable = current.FirstOrDefault(x => x.Name == variableName);

                if (variable != null)
                {
                    break;
                }
            }

            return variable != null;
        }

        public IEvaluationScope CreateClosureOver(IJsonToken token)
        {
            _closures.Push(token);

            return this;
        }

        public IEvaluationScope AddOrUpdateVariable(RangeVariable variable, bool forceApplyToCurrentLayer = false)
        {
            if (_variables.Count == 0)
            {
                // No variable scopes currently exist, so we just need to create one.

                _variables.Push(new List<RangeVariable>());
            }
            
            var currentLayers = new Stack<IList<RangeVariable>>(_variables);

            while (currentLayers.TryPop(out var current))
            {
                var existingIndex = current.IndexOf(x => x.Name == variable.Name);

                if (existingIndex is null)
                {
                    continue;
                }

                current[existingIndex.Value] = variable;

                return this;
            }

            if (!forceApplyToCurrentLayer)
            {
                // All variables in the outer scope are still accessible in the new scope but
                // if they happen to exit the current scope we can just drop the layer once we're
                // done and call it good.

                _variables.Push(new List<RangeVariable>());
            }

            _variables.Peek().Add(variable);

            return this;
        }

        public bool ContainsVariable(string variableName) => _variables.Peek().Any(x => x.Name == variableName);

        public IEvaluationScope RemoveCurrentClosure()
        {
            _closures.Pop();

            return this;
        }

        public IEvaluationScope RemoveCurrentVariablesLayer()
        {
            _variables.Pop();

            return this;
        }
    }
}
