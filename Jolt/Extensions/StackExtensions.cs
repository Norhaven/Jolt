using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class StackExtensions
    {
        public static bool TryPop<T>(this Stack<T> stack, out T? value) where T : class
        {
#if NETSTANDARD2_0
            if (stack.Count == 0)
            {
                value = null;
                return false;
            }

            value = stack.Pop();

            return true;
#else
            return stack.TryPop(out value);
#endif
        }
    }
}
