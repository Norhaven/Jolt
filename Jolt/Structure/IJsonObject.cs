using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonObject : IJsonToken, IEnumerable<IJsonProperty>
    {
        IJsonToken? this[string propertyName] { get; set; }

        IJsonToken? Remove(string propertyName);
    }
}
