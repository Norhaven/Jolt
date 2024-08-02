using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a way to determine whether a particular library method is valid to be used
    /// within the property name, property value, or both.
    /// </summary>
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
