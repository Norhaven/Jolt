using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Jolt.Structure
{
    internal sealed class ValidationScope
    {
        private readonly ImmutableHashSet<string> _variables;

        public static readonly ValidationScope Empty = new ValidationScope(ImmutableHashSet<string>.Empty);

        private ValidationScope(ImmutableHashSet<string> variables)
        {
            _variables = variables;
        }

        public bool IsVariableDeclared(string name) => _variables.Contains(name);

        public ValidationScope With(string variableName) =>
            new ValidationScope(_variables.Add(variableName));

        public ValidationScope With(IEnumerable<string> names) =>
            new ValidationScope(_variables.Union(names));
    }
}
