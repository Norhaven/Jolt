using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    /// <summary>
    /// Represents a way of registering and resolving methods for uses within a Jolt transformer.
    /// </summary>
    public interface IMethodReferenceResolver
    {
        /// <summary>
        /// Gets a method signature by name.
        /// </summary>
        /// <param name="methodName">The unique name of the method to retrieve. If aliased, this will be the alias value.</param>
        /// <returns>An instance of <see cref="MethodSignature"/> if found, null otherwise.</returns>
        MethodSignature? GetMethod(string methodName);

        /// <summary>
        /// Registers the provided method registrations. If any are instance methods, the method context instance must also be provided.
        /// </summary>
        /// <param name="methodRegistrations">The series of registrations to include.</param>
        /// <param name="methodContext">The context that any instance methods will operate within.</param>
        void RegisterMethods(IEnumerable<MethodRegistration> methodRegistrations, object? methodContext = default);

        /// <summary>
        /// Removes all external method registrations. This will keep any library methods that have already been registered.
        /// </summary>
        void Clear();
    }
}
