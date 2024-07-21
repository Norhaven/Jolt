using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IQueryPathProvider
    {
        IJsonToken? SelectNodeAtPath(Stack<IJsonToken> closureSources, string path, JsonQueryMode queryMode);
        IJsonToken? GetRootNodeFrom(Stack<IJsonToken> closureSources, JsonQueryMode queryMode);
    }
}
