using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonToken
    {
        IJsonToken? Parent { get; }
        JsonTokenType Type { get; }

        IJsonObject AsObject();
        IJsonArray AsArray();
        IJsonValue AsValue();
        IJsonToken? SelectTokenAtPath(string path);
        IJsonToken? Copy();
        void Clear();
        T ToTypeOf<T>();
    }
}
