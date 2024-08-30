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
        public IEnumerable<RangeVariable> AvailableVariables => _variables.Peek();
        public int VariableCount => _variables.Count == 0 ? 0 : _variables.Peek().Count;

        public EvaluationScope(Stack<IJsonToken> closures, Stack<IList<RangeVariable>> variables)
        {
            _closures = closures;
            _variables = variables;
        }

        public bool TryGetVariable(string variableName, out RangeVariable? variable)
        {
            if (_variables.Count == 0)
            {
                variable = default;
                return false;
            }

            variable = _variables.Peek().FirstOrDefault(x => x.Name == variableName);

            return variable != null;
        }

        public IEvaluationScope CreateClosureOver(IJsonToken token)
        {
            _closures.Push(token);

            return this;
        }

        public IEvaluationScope AddOrUpdateVariable(RangeVariable variable, bool forceApplyToCurrent = false)
        {
            if (_variables.Count == 0)
            {
                // No variable scopes currently exist, so we just need to create one.

                _variables.Push(new List<RangeVariable>());
            }
            else if (!forceApplyToCurrent)
            {
                // All variables in the outer scope are still accessible in the new scope but
                // if they happen to add more we can just drop the layer once we're
                // done and call it good.

                _variables.Push(_variables.Peek().Select(x => x).ToList());
            }

            var current = _variables.Peek();

            for (var i = 0; i < current.Count; i++)
            {
                var currentVariable = current[i];

                if (currentVariable.Name == variable.Name)
                {
                    current[i] = variable;

                    return this;
                }
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

        public IEvaluationScope RemoveCurrentVariables()
        {
            _variables.Pop();

            return this;
        }
    }
}
