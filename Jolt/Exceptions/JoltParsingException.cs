﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public sealed class JoltParsingException : JoltException
    {
        public JoltParsingException(ExceptionCode code, string message, JoltException? innerException = default)
            : base(code, message, innerException) { }
    }
}
