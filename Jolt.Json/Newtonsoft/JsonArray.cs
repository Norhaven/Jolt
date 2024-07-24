using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public class JsonArray : JsonToken, IJsonArray
    {
        private readonly IList<IJsonToken> _arrayElements;

        public IJsonToken? this[int index] 
        {
            get => _arrayElements[index];
            set => _arrayElements[index] = value;
        }

        public int Length => _arrayElements.Count;

        public JsonArray(JToken? token) 
            : base(token)
        {
            _arrayElements = ((JArray)token).Select(x => FromObject(x)).ToList();
        }

        public void Add(IJsonToken? token) => _arrayElements.Add(token);

        public IEnumerator<IJsonToken> GetEnumerator() => _arrayElements.GetEnumerator();

        public IJsonToken? RemoveAt(int index)
        {
            var element = this[index];

            _arrayElements.RemoveAt(index);

            return element;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
