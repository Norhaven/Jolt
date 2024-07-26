using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    internal class JoltLibraryMethodAttribute : Attribute
    {
        public string Name { get; }
        public bool IsValueGenerator { get; }

        public JoltLibraryMethodAttribute(string name, bool isValueGenerator = false)
        {
            Name = name;
            IsValueGenerator = isValueGenerator;
        }
    }
}
