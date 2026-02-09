using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class TypeExtensions
    {
        public static bool IsDelegate(this Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            var definition = type.GetGenericTypeDefinition();

            return definition == typeof(Func<,>);
        }
    }
}
