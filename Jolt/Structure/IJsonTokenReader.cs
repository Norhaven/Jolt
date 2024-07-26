using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonTokenReader
    {
        IJsonToken? Read(string json);
        IJsonToken? CreateArrayFrom(IEnumerable<IJsonToken> tokens);
        IJsonToken? CreateTokenFrom(object? value);
        IJsonToken? CreateObjectFrom(IEnumerable<IJsonToken>? tokens);
    }
}
