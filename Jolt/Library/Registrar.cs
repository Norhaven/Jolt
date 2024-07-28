using Jolt.Exceptions;
using Jolt.Parsing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Library
{
    internal static class Registrar
    {
        public static IEnumerable<MethodSignature> GetStandardLibraryRegistrations()
        {
            var type = typeof(StandardLibraryMethods);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            return from method in methods
                   let attribute = method.GetCustomAttribute<JoltLibraryMethodAttribute>()
                   where attribute != null
                   let parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, x.GetCustomAttribute<LazyEvaluationAttribute>() != null))
                   select new MethodSignature(type.AssemblyQualifiedName, method.Name, attribute.Name, method.ReturnType, CallType.Static, true, attribute.IsValueGenerator, parameters.ToArray());
        }

        public static IEnumerable<MethodSignature> GetExternalMethodRegistrations(IEnumerable<MethodRegistration> registrations, object? methodContext = default)
        {
            return registrations.Select(x => GetExternalMethodRegistration(x, methodContext));
        }

        public static MethodSignature GetExternalMethodRegistration(MethodRegistration registration, object? methodContext = default)
        {
            if (string.IsNullOrWhiteSpace(registration.FullyQualifiedTypeName))
            {
                if (methodContext is null)
                {
                    throw new JoltMethodResolutionException(default, registration.MethodName, $"Unable to locate instance method '{registration.MethodName}' during method resolution, no method context was provided");
                }

                var type = methodContext.GetType();
                var method = type.GetMethod(registration.MethodName);
                var parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, false)).ToArray();

                return new MethodSignature(type?.AssemblyQualifiedName, registration.MethodName, registration.Alias, method?.ReturnType, registration.CallType, false, false, parameters);
            }
            else
            {
                var type = Type.GetType(registration.FullyQualifiedTypeName);
                var method = type.GetMethod(registration.MethodName);
                var parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, false)).ToArray();

                return new MethodSignature(type.AssemblyQualifiedName, registration.MethodName, registration.Alias, method.ReturnType, registration.CallType, false, false, parameters);
            }
        }
    }
}
