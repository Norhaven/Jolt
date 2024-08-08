using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt
{
    /// <summary>
    /// Represents the context that a JSON transformation occurs within. This includes the JSON transformer itself,
    /// external method registrations, and implementations of the tools that will be used during the process.
    /// </summary>
    public interface IJsonContext
    {
        /// <summary>
        /// The JSON transformer that will be used to evaluate a 
        /// source JSON document in order to apply a transformation to it.
        /// </summary>
        string JsonTransformer { get; }

        /// <summary>
        /// A way of reading intermediary JSON objects from different sources.
        /// </summary>
        IJsonTokenReader JsonTokenReader { get; }

        /// <summary>
        /// A way of reading a Jolt expression as tokens for further evaluation.
        /// </summary>
        ITokenReader TokenReader { get; }

        /// <summary>
        /// A way of parsing a series of Jolt expression tokens into an expression tree for evaluation.
        /// </summary>
        IExpressionParser ExpressionParser { get; }

        /// <summary>
        /// A way of evaluating a Jolt expression tree and executing it according to the language rules.
        /// </summary>
        IExpressionEvaluator ExpressionEvaluator { get; }

        /// <summary>
        /// A provider for JSON queries against a JSON structure.
        /// </summary>
        IQueryPathProvider QueryPathProvider { get; }

        /// <summary>
        /// External method registrations. These are user-defined and can be invoked during the transformation process.
        /// </summary>
        MethodRegistration[] MethodRegistrations { get; }

        /// <summary>
        /// Maintains all method registrations in order to allow easier lookup of method signatures during evaluation. 
        /// </summary>
        IMethodReferenceResolver ReferenceResolver { get; }

        /// <summary>
        /// An instance that will be used to invoke any external instance methods that have been registered and called from the transformer.
        /// </summary>
        object? MethodContext { get; }

        /// <summary>
        /// Registers a single external method for use.
        /// </summary>
        /// <param name="method">The external method registration.</param>
        /// <returns>An instance of <see cref="IJsonContext"/> with the registration applied to it.</returns>
        IJsonContext RegisterMethod(MethodRegistration method);

        /// <summary>
        /// Registers a series of external methods for use.
        /// </summary>
        /// <param name="method">The external method registrations.</param>
        /// <returns>An instance of <see cref="IJsonContext"/> with the registrations applied to it.</returns>
        IJsonContext RegisterAllMethods(IEnumerable<MethodRegistration> methods);

        /// <summary>
        /// Registers one or more external methods for use. These must be marked with the <see cref="JoltExternalMethodAttribute"/> attribute
        /// in order to actually be registered.
        /// </summary>
        /// <returns>An instance of <see cref="IJsonContext"/> with the registrations applied to it.</returns>
        IJsonContext RegisterAllMethodsFrom<T>();

        /// <summary>
        /// Specifies the JSON transformer that will be used.
        /// </summary>
        /// <param name="jsonTransformer">The JSON transformer.</param>
        /// <returns>An instance of <see cref="IJsonContext"/> with the transformer usage applied to it.</returns>
        IJsonContext UseTransformer(string jsonTransformer);

        /// <summary>
        /// Specifies the method context that will be used for registered instance method calls.
        /// </summary>
        /// <param name="methodContext">The method context instance.</param>
        /// <returns>An instance of <see cref="IJsonContext"/> with the method context usage applied to it.</returns>
        IJsonContext UseMethodContext(object? methodContext);
    }
}
