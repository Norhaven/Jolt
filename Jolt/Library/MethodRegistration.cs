using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a registration for a method. This encapsulates the information needed in order for the Jolt evaluator
    /// to invoke the method correctly during transformation.
    /// </summary>
    public sealed class MethodRegistration
    {
        /// <summary>
        /// Creates a method registration for the named static method found on the type of <typeparamref name="T"/>
        /// and optionally creates an alias for the method (used in the JSON transformer to identify the method to invoke).
        /// </summary>
        /// <typeparam name="T">The type that contains the named method.</typeparam>
        /// <param name="methodName">The method to register.</param>
        /// <param name="alias">The alias of the named method.</param>
        /// <returns>An instance of <see cref="MethodRegistration"/>.</returns>
        public static MethodRegistration FromStaticMethod<T>(string methodName, string alias = default) => FromStaticMethod(typeof(T), methodName, alias);

        /// <summary>
        /// Creates a method registration for the named static method found on the type passed as a parameter
        /// and optionally creates an alias for the method (used in the JSON transformer to identify the method to invoke).
        /// </summary>
        /// <param name="type">The type that contains the named method.</param>
        /// <param name="methodName">The method to register.</param>
        /// <param name="alias">The alias of the named method.</param>
        /// <returns>An instance of <see cref="MethodRegistration"/>.</returns>
        public static MethodRegistration FromStaticMethod(Type type, string methodName, string alias = default) => new MethodRegistration(type.AssemblyQualifiedName, methodName, alias);

        /// <summary>
        /// Creates a method registration for the named instance method found on the type used as the method context
        /// and optionally creates an alias for the method (used in the JSON transformer to identify the method to invoke).
        /// </summary>
        /// <param name="methodName">The method to register.</param>
        /// <param name="alias">The alias of the named method.</param>
        /// <returns>An instance of <see cref="MethodRegistration"/>.</returns>
        public static MethodRegistration FromInstanceMethod(string methodName, string alias = default) => new MethodRegistration(methodName, alias);

        /// <summary>
        /// Gets the assembly-qualified type name that contains the method.
        /// </summary>
        public string FullyQualifiedTypeName { get; }

        /// <summary>
        /// Gets the name of the method to register.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets the type of call needed to invoke this method.
        /// </summary>
        public CallType CallType { get; }

        /// <summary>
        /// Gets the alias for the method to register.
        /// </summary>
        public string Alias { get; }

        /// <summary>
        /// Initializes an instance of <see cref="MethodRegistration"/> with the provided parameters as a static method.
        /// </summary>
        /// <param name="assemblyQualifiedTypeName">The assembly-qualified type name that contains this method.</param>
        /// <param name="staticMethodName">The name of the static method to register.</param>
        /// <param name="alias">The alias for the method to register.</param>
        public MethodRegistration(string assemblyQualifiedTypeName, string staticMethodName, string alias)
        {
            FullyQualifiedTypeName = assemblyQualifiedTypeName;
            MethodName = staticMethodName;
            CallType = CallType.Static;
            Alias = alias;
        }

        /// <summary>
        /// Initializes an instance of <see cref="MethodRegistration"/> with the provided parameters as an instance method.
        /// </summary>
        /// <param name="instanceMethodName">The name of the instance method to register.</param>
        /// <param name="alias">The alias for the method to register.</param>
        public MethodRegistration(string instanceMethodName, string alias)
        {
            FullyQualifiedTypeName = string.Empty;
            MethodName = instanceMethodName;
            Alias = alias;
            CallType = CallType.Instance;
        }
    }
}
