using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Structure
{
    public abstract class QueryPathProvider : IQueryPathProvider
    {
        public IJsonToken? GetRootNodeFrom(IEnumerable<IJsonToken> closureSources, JsonQueryMode queryMode)
        {
            var sourceNode = queryMode switch
            {
                JsonQueryMode.StartFromRoot => closureSources.FirstOrDefault(),
                JsonQueryMode.StartFromClosestMatch => closureSources.LastOrDefault()
            };

            IJsonToken parent;

            for (parent = sourceNode.Parent; parent != null; parent = parent.Parent)
            {
            }

            return parent ?? sourceNode;
        }

        public abstract bool IsQueryPath(string path);

        public abstract IJsonToken? SelectNodeAtPath(IEnumerable<IJsonToken> closureSources, string path, JsonQueryMode queryMode);
    }
}
