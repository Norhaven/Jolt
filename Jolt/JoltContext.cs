using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt
{
    public class JoltContext : IJsonContext
    {
        public string JsonTransformer { get; }

        public IExpressionParser ExpressionParser { get; }

        public IExpressionEvaluator ExpressionEvaluator { get; }

        public ITokenReader TokenReader { get; }

        public IJsonTokenReader JsonTokenReader { get; }

        public IQueryPathProvider QueryPathProvider { get; }

        public MethodRegistration[] MethodRegistrations { get; }

        public IReferenceResolver ReferenceResolver { get; }

        public JoltContext(string jsonTransformer, 
                           IExpressionParser expressionParser, 
                           IExpressionEvaluator expressionEvaluator, 
                           ITokenReader tokenReader, 
                           IJsonTokenReader jsonTokenReader,
                           IQueryPathProvider queryPathProvider,
                           IReferenceResolver referenceResolver)
        {
            JsonTransformer = jsonTransformer;
            ExpressionParser = expressionParser;
            ExpressionEvaluator = expressionEvaluator;
            TokenReader = tokenReader;
            JsonTokenReader = jsonTokenReader;
            QueryPathProvider = queryPathProvider;
            ReferenceResolver = referenceResolver;
        }

        public IJsonContext RegisterMethod(MethodRegistration method)
        {
            throw new NotImplementedException();
        }

        public IJsonContext RegisterAllMethods(IEnumerable<MethodRegistration> methods)
        {
            throw new NotImplementedException();
        }

        public IJsonContext UseTransformer(string jsonTransformer)
        {
            throw new NotImplementedException();
        }
    }
}
