using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MethodIsValidOnAttribute : Attribute
    {
        public LibraryMethodTarget Target { get; }

        public MethodIsValidOnAttribute(LibraryMethodTarget target)
        {
            Target = target;
        }
    }
}
