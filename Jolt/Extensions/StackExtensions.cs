using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Extensions
{
    internal static class StackExtensions
    {
        public static Stack<T> Copy<T>(this Stack<T> stack)
        {
            return new Stack<T>(stack.Reverse());
        }

        public static T PopUntilRoot<T>(this Stack<T> stack)
        {
            T root = default;

            while(stack.Count > 0)
            {
                root = stack.Pop();
            }

            return root;
        }
    }
}
