using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Newtonsoft.Extensions
{
    internal static class JsonExtensions
    {
        public static void SetEnclosingPropertyValue(this JToken? token, object? value)
        {
            var property = token.GetEnclosingProperty();

            property.Value = value is null ? JValue.CreateNull() : JToken.FromObject(value);
        }

        public static JProperty GetEnclosingProperty(this JToken? token)
        {
            if (token is null)
            {
                return default;
            }

            if (token is JProperty property)
            {
                return property;
            }

            return token.Parent.GetEnclosingProperty();
        }
    }
}
