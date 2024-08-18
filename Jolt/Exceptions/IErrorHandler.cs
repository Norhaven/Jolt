using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public interface IErrorHandler
    {
        bool IsEnabled { get; }

        void HandleFor<T>(Exception exception);
    }
}
