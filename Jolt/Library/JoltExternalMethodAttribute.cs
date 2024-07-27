using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    public class JoltExternalMethodAttribute : Attribute
    {
        public string Name { get; }

        public JoltExternalMethodAttribute(string name)
        {
            Name = name;
        }
    }
}
