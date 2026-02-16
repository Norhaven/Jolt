using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonEqualityComparer
    {
        Type ApplicableType { get; }

        bool AreEqual(IJsonToken? token, IJsonToken? other);
    }
}
