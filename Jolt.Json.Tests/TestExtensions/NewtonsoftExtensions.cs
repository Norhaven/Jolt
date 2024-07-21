using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestExtensions;

internal static class NewtonsoftExtensions
{
    public static T PropertyValueFor<T>(this JObject json, string propertyName) => (T)Convert.ChangeType(((JValue)json?[propertyName])?.Value, typeof(T));
}
