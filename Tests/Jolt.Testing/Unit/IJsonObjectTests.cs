using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Jolt.Testing.Unit
{
    public abstract class IJsonObjectTests
    {
        protected abstract IJsonContext CreateContext();

        protected IJsonObject CreateObjectFromDictionary(Dictionary<string, string> data)
        {
            var context = CreateContext();

            var jsonToken = context.JsonTokenReader.CreateTokenFrom(data);

            return jsonToken.AsObject();
        }

        protected Dictionary<string, string> CreateDictionaryFromObject(IJsonObject jsonObject)
        {
            return jsonObject.ToTypeOf<Dictionary<string, string>>();
        }

        protected string ToJsonString(IJsonObject jsonObject)
        {
            return jsonObject.ToTypeOf<string>();
        }
    }
}
