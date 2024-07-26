using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public class JsonObject : JsonToken, IJsonObject
    {
        private readonly IDictionary<string, IJsonToken> _properties = new Dictionary<string, IJsonToken>();

        public IJsonToken? this[string propertyName] 
        {
            get => _properties[propertyName];
            set
            {
                _properties[propertyName] = value;
                ((JObject)_token)[propertyName] = value.ToTypeOf<JToken>();
            }
        }

        public JsonObject(JToken? token) 
            : base(token)
        {
            _properties = ((JObject)_token).Properties().ToDictionary(x => x.Name, x => FromObject(x.Value));
        }

        public IJsonToken? Remove(string propertyName)
        {
            if (!_properties.TryGetValue(propertyName, out var propertyToken))
            {
                return default;
            }

            _properties.Remove(propertyName);
            ((JObject)_token).Remove(propertyName);

            return propertyToken;
        }

        public IEnumerator<IJsonProperty> GetEnumerator() => ((JObject)_token).Properties().Select(x => (IJsonProperty)FromObject(x)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override void Clear()
        {
            _properties.Clear();
            ((JObject)_token).RemoveAll();
        }
    }
}
