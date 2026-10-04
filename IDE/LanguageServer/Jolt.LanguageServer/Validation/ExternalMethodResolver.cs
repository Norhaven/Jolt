using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;

namespace Jolt.LanguageServer.Validation
{
    /// <summary>
    /// Resolves the standard library along with the external methods that the client says a host application provides.
    /// External methods are only known by name, so each one resolves to a stand-in signature that accepts any number of
    /// arguments and is only valid in a property value (as with every external method). This keeps them from being
    /// reported as unknown, and lets the rest of an expression that calls them be validated.
    /// </summary>
    internal sealed class ExternalMethodResolver : IMethodReferenceResolver
    {
        // Reading the standard library reflects over the Jolt assembly, so it is done once and shared. Resolving a
        // method only reads from it, which is safe to do from validations running at the same time.
        private static readonly Lazy<MethodReferenceResolver> _standardLibrary =
            new Lazy<MethodReferenceResolver>(() => new MethodReferenceResolver(new MessageProvider(JoltOptions.Default)));

        private readonly Dictionary<string, MethodSignature> _externalMethods = new Dictionary<string, MethodSignature>(StringComparer.Ordinal);

        public ExternalMethodResolver(IEnumerable<string> externalMethodNames)
        {
            foreach (var name in externalMethodNames)
            {
                _externalMethods[name] = CreateSignature(name);
            }
        }

        public MethodSignature? GetMethod(string methodName)
        {
            // Jolt calls a standard library method in preference to an external method with the same name.
            return _standardLibrary.Value.GetMethod(methodName) ?? (_externalMethods.TryGetValue(methodName, out var method) ? method : null);
        }

        public void RegisterMethods(IEnumerable<MethodRegistration> methodRegistrations, object? methodContext = default)
        {
            throw new NotSupportedException("External methods are provided by name when the resolver is created, since their implementations are not available to the language server.");
        }

        public void Clear()
        {
            _externalMethods.Clear();
        }

        private static MethodSignature CreateSignature(string name)
        {
            var arguments = new MethodParameter(typeof(object), "arguments", isLazyEvaluated: false, isVariadic: true, isOptional: false, optionalDefaultValue: null, isDelegate: false);

            return new MethodSignature(
                assemblyQualifiedTypeName: string.Empty,
                name: name,
                alias: name,
                returnType: typeof(object),
                callType: CallType.Static,
                isSystemMethod: false,
                isValueGenerator: false,
                isAllowedAsPropertyName: false,
                isAllowedAsPropertyValue: true,
                isAllowedAsStatement: false,
                isAllowedAsMatchCase: false,
                isUnsafe: false,
                arguments);
        }
    }
}
