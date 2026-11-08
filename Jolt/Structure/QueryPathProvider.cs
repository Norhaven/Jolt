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

        public abstract bool IsValidQueryPath(string path);

        public abstract bool CanMatchMultipleValues(string path);

        public IJsonToken? SelectNodeAtPath(IEnumerable<IJsonToken> closureSources, string path, JsonQueryMode queryMode)
        {
            if (queryMode == JsonQueryMode.StartFromRoot)
            {
                return SelectFrom(GetRootNodeFrom(closureSources, queryMode), path, isSourceDocument: true);
            }

            if (queryMode != JsonQueryMode.StartFromClosestMatch)
            {
                throw new ArgumentOutOfRangeException(nameof(queryMode), $"Unable to select JSON node, found unsupported query mode '{queryMode}'");
            }

            // The search goes outward from the closest source. A path that can match several values always gives an array,
            // so an empty one means that this source had nothing and it's only the result if none of the others do either.

            var sources = closureSources.ToArray();
            var emptyResult = default(IJsonToken);

            for (var i = sources.Length - 1; i >= 0; i--)
            {
                var result = SelectFrom(sources[i], path, isSourceDocument: i == 0);

                if (result is null)
                {
                    continue;
                }

                if (!(result is IJsonArray array) || array.Length > 0 || !CanMatchMultipleValues(path))
                {
                    return result;
                }

                emptyResult = result;
            }

            return emptyResult;
        }

        /// <summary>
        /// Selects from a single source. The source document doesn't change during a transformation, unlike any other token, which a provider can rely on.
        /// </summary>
        protected virtual IJsonToken? SelectFrom(IJsonToken source, string path, bool isSourceDocument) => source.SelectTokenAtPath(path);
    }
}
