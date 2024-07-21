using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonNodeReader
    {
        IJsonNode Read(string json);
    }
}
