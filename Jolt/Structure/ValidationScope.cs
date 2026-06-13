using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Represents the current scope of validation for a given expression and provides access to determine if a variable is present during evaluation.
    /// </summary>
    internal sealed class ValidationScope
    {
        private readonly ImmutableHashSet<string> _variables;

        /// <summary>
        /// Gets an empty validation scope with no variables declared.
        /// </summary>
        public static readonly ValidationScope Empty = new ValidationScope(ImmutableHashSet<string>.Empty);

        private ValidationScope(ImmutableHashSet<string> variables)
        {
            _variables = variables;
        }

        /// <summary>
        /// Determines whether a named variable is declared within the current scope.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>True if the variable name exists in the scope, false otherwise.</returns>
        public bool IsVariableDeclared(string name) => _variables.Contains(name);

        /// <summary>
        /// Gets a new validation scope with the specified variable name added to the current scope.
        /// </summary>
        /// <param name="variableName">The new variable name.</param>
        /// <returns>The scope with the variable name contained within it.</returns>
        public ValidationScope With(string variableName) =>
            new ValidationScope(_variables.Add(variableName));

        /// <summary>
        /// Gets a new validation scope with the specified variable names added to the current scope.
        /// </summary>
        /// <param name="names">The new variable names.</param>
        /// <returns>The scope with the variable names contained within it.</returns>
        public ValidationScope With(IEnumerable<string> names) =>
            new ValidationScope(_variables.Union(names));
    }
}
