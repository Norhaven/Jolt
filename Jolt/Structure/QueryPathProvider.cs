using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public abstract class QueryPathProvider : IQueryPathProvider
    {
        public IJsonToken? GetRootNodeFrom(Stack<IJsonToken> closureSources, JsonQueryMode queryMode)
        {
            var sourceNode = queryMode switch
            {
                JsonQueryMode.StartFromRoot => closureSources.Copy().PopUntilRoot(),
                JsonQueryMode.StartFromClosestMatch => closureSources.Peek()
            };

            IJsonToken parent;

            for (parent = sourceNode.Parent; parent != null; parent = parent.Parent)
            {
            }

            return parent ?? sourceNode;
        }

        public abstract IJsonToken? SelectNodeAtPath(Stack<IJsonToken> closureSources, string path, JsonQueryMode queryMode);
    }
}
