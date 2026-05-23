using Jolt.Exceptions;
using Jolt.Extensions;
using Jolt.Parsing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Library
{
    internal class Registrar
    {
        public static IEnumerable<MethodSignature> GetStandardLibraryRegistrations()
        {
            var assembly = typeof(Registrar).Assembly;

            return from type in assembly.GetTypes().Where(x => x.GetCustomAttribute<IncludeInStandardLibraryAttribute>() != null)
                   from method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                   let attribute = method.GetCustomAttribute<JoltLibraryMethodAttribute>()
                   where attribute != null
                   let validity = method.GetCustomAttribute<MethodIsValidOnAttribute>()
                   let parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, x.GetCustomAttribute<LazyEvaluationAttribute>() != null, x.GetCustomAttribute<VariadicEvaluationAttribute>() != null, x.GetCustomAttribute<OptionalParameterAttribute>() != null, x.GetCustomAttribute<OptionalParameterAttribute>()?.DefaultValue, x.ParameterType.IsDelegate()))
                   select new MethodSignature(type.AssemblyQualifiedName, method.Name, attribute.Name, method.ReturnType, CallType.Static, true, attribute.IsValueGenerator, validity.Target.HasFlag(LibraryMethodTarget.PropertyName), validity.Target.HasFlag(LibraryMethodTarget.PropertyValue), validity.Target.HasFlag(LibraryMethodTarget.StatementBlock), attribute.IsUnsafe, parameters.ToArray());
        }

        public static IEnumerable<MethodSignature> GetExternalMethodRegistrations(IEnumerable<MethodRegistration> registrations, IMessageProvider messageProvider, object? methodContext = default)
        {
            return registrations.Select(x => GetExternalMethodRegistration(x, messageProvider, methodContext));
        }

        public static MethodSignature GetExternalMethodRegistration(MethodRegistration registration, IMessageProvider messageProvider, object? methodContext = default)
        {
            if (string.IsNullOrWhiteSpace(registration.FullyQualifiedTypeName))
            {
                if (methodContext is null)
                {
                    throw messageProvider.CreateResolutionErrorFor<Registrar>(ExceptionCode.UnableToLocateInstanceMethod, default, registration.MethodName, registration.MethodName);
                }

                var type = methodContext.GetType();
                var method = type.GetMethod(registration.MethodName);

                if (method is null)
                {
                    throw messageProvider.CreateResolutionErrorFor<Registrar>(ExceptionCode.UnableToLocateInstanceMethodWithProvidedMethodContext, type.FullName, registration.MethodName, registration.MethodName, type.FullName);
                }

                var parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, false, false, false, null, x.ParameterType.IsDelegate())).ToArray();

                return new MethodSignature(type?.AssemblyQualifiedName, registration.MethodName, registration.Alias, method?.ReturnType, registration.CallType, false, false, false, true, false, false, parameters);
            }
            else
            {
                var type = Type.GetType(registration.FullyQualifiedTypeName);

                if (type is null)
                {
                    throw messageProvider.CreateResolutionErrorFor<Registrar>(ExceptionCode.UnableToLocateTypeForStaticMethod, registration.FullyQualifiedTypeName, registration.MethodName, registration.FullyQualifiedTypeName, registration.MethodName);
                }

                var method = type.GetMethod(registration.MethodName);

                if (method is null)
                {
                    throw messageProvider.CreateResolutionErrorFor<Registrar>(ExceptionCode.UnableToLocateStaticMethodWithProvidedType, type.FullName, registration.MethodName, registration.MethodName, type.FullName);
                }

                var parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, false, false, false, null, x.ParameterType.IsDelegate())).ToArray();

                return new MethodSignature(type.AssemblyQualifiedName, registration.MethodName, registration.Alias, method.ReturnType, registration.CallType, false, false, false, true, false, false, parameters);
            }
        }
    }
}
