using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonValue : IJsonToken
    {
        JsonValueType ValueType { get; }

        T AsObject<T>();
    }
}
