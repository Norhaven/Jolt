using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    internal class JoltLibrarySystemParameterAttribute : Attribute
    {
        public SystemParameterType ParameterType { get; }

        public JoltLibrarySystemParameterAttribute(SystemParameterType parameterType)
        {
            ParameterType = parameterType;
        }
    }
}
