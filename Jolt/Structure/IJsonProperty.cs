using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonProperty : IJsonToken
    {
        public string PropertyName { get; }
        public IJsonToken? Value { get; }
    }
}
