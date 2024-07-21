﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonObject : IJsonToken, IEnumerable<KeyValuePair<string, IJsonToken>>
    {
        IJsonToken? this[string propertyName] { get; set; }

        IJsonToken? Remove(string propertyName);
    }
}
