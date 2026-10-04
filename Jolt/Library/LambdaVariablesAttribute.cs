using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a marker that describes the variables a lambda parameter binds when it is evaluated, which also
    /// determines its arity (e.g. "acc" and "current" for a two-parameter lambda written as @acc;@current: ...).
    /// This is descriptive only and is used to generate language metadata for IDE tooling.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class LambdaVariablesAttribute : Attribute
    {
        public string[] VariableNames { get; }

        public LambdaVariablesAttribute(params string[] variableNames)
        {
            VariableNames = variableNames;
        }
    }
}
