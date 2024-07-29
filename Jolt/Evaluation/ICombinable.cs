using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public interface ICombinable
    {
        object? CombineWith(object? value);
    }
}
