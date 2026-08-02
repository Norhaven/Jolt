using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class TypeExtensions
    {
        // This is a list of all of the arities of Func<> that Jolt supports for lambda expressions.
        // At the moment, anything above two parameters and a result doesn't fall within the
        // common use cases of the language. For example, reduce/zip/etc only need that. In the event
        // that a valid use case for higher arities is required, we can expand this validation here.

        private static readonly HashSet<Type> _funcTypeDefinitions = new HashSet<Type>()
        {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>)
        };

        public static bool IsAllowedDelegate(this Type type)
        {
            return IsFuncType(type, out _);
        }        

        private static bool IsFuncType(Type type, out int arity)
        {
            arity = 0;

            if (!type.IsGenericType)
            {
                return false;
            }

            var definition = type.GetGenericTypeDefinition();

            if (!_funcTypeDefinitions.Contains(definition))
            {
                return false;
            }

            arity = type.GetGenericArguments().Length - 1; // Omit the last argument because it's the result type.

            return true;
        }
    }
}
