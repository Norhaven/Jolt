using Jolt.Parsing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Library
{
    public static class Registrar
    {
        public static IEnumerable<MethodSignature> RegisterStandardLibrary()
        {
            var type = typeof(StandardMethods);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            return from method in methods
                   let attribute = method.GetCustomAttribute<JoltLibraryMethodAttribute>()
                   where attribute != null
                   let parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name, x.GetCustomAttribute<JoltLibrarySystemParameterAttribute>()?.ParameterType ?? SystemParameterType.Unknown))
                   select new MethodSignature(Assembly.GetExecutingAssembly().FullName, type.FullName, method.Name, attribute.Name, method.ReturnType, CallType.Static, true, parameters.ToArray());
        }

        public static MethodSignature Register(MethodRegistration registration)
        {
            var hasAssemblyName = registration.FullyQualifiedTypeName.Contains(',');

            var assemblyPieces = hasAssemblyName ? registration.FullyQualifiedTypeName.Split(',') : Array.Empty<string>();
            var namespacePieces = hasAssemblyName ? assemblyPieces[1].Split('.') : registration.FullyQualifiedTypeName.Split('.');

            var assemblyName = hasAssemblyName ? assemblyPieces[0] : string.Empty;
            var methodName = namespacePieces[^1];
            var typeName = string.Join('.', namespacePieces[..^1]);

            var type = Type.GetType(hasAssemblyName ? $"{assemblyName},{typeName}" : typeName);
            var method = type.GetMethod(methodName);
            var callType = method.IsStatic ? CallType.Static : CallType.Instance;
            var parameters = method.GetParameters().Select(x => new MethodParameter(x.ParameterType, x.Name)).ToArray();

            return new MethodSignature(assemblyName, typeName, methodName, methodName, method.ReturnType, callType, false, parameters);
        }
    }
}
