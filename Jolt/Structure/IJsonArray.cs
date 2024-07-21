using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonArray : IJsonToken, IEnumerable<IJsonToken>
    {
        IJsonToken? this[int index] { get; set; }

        IJsonToken? RemoveAt(int index);
        void Add(IJsonToken? token);
    }
}
