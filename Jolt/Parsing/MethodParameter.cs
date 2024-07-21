using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public class MethodParameter
    {
        public Type Type { get; }
        public string Name { get; }
        internal SystemParameterType ParameterType { get; }

        internal MethodParameter(Type type, string name, SystemParameterType parameterType)
            :this(type, name)
        {
            ParameterType = parameterType;  
        }

        public MethodParameter(Type type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}
