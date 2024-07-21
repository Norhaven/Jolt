using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public class JsonPathQueryPathProvider : QueryPathProvider
    {
        public override IJsonToken? SelectNodeAtPath(Stack<IJsonToken> source, string queryPath, JsonQueryMode queryMode)
        {
            var queryToken = queryMode switch
            {
                JsonQueryMode.StartFromRoot => GetRootNodeFrom(source, queryMode),
                JsonQueryMode.StartFromClosestMatch => source.Peek(),
                _ => throw new ArgumentOutOfRangeException(nameof(queryPath), $"Unable to select JSON node, found unsupported query mode '{queryMode}'")
            };

            return queryToken.SelectTokenAtPath(queryPath);
        }
    }
}
