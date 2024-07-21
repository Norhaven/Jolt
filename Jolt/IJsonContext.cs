using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt
{
    public interface IJsonContext
    {
        string JsonTransformer { get; }
        IJsonTokenReader JsonTokenReader { get; }
        ITokenReader TokenReader { get; }
        IExpressionParser ExpressionParser { get; }
        IExpressionEvaluator ExpressionEvaluator { get; }
        IQueryPathProvider QueryPathProvider { get; }
        MethodRegistration[] MethodRegistrations { get; }

        IJsonContext RegisterMethod(MethodRegistration method);
        IJsonContext RegisterAllMethods(IEnumerable<MethodRegistration> methods);
        IJsonContext UseTransformer(string jsonTransformer);
    }
}
