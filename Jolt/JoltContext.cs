using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt
{
    public class JoltContext : IJsonContext
    {
        public string JsonTransformer { get; private set; }

        public IExpressionParser ExpressionParser { get; }

        public IExpressionEvaluator ExpressionEvaluator { get; }

        public ITokenReader TokenReader { get; }

        public IJsonTokenReader JsonTokenReader { get; }

        public IQueryPathProvider QueryPathProvider { get; }

        public MethodRegistration[] MethodRegistrations { get; private set; } = Array.Empty<MethodRegistration>();

        public IMethodReferenceResolver ReferenceResolver { get; }

        public object? MethodContext { get; }

        public JoltContext(string jsonTransformer, 
                           IExpressionParser expressionParser, 
                           IExpressionEvaluator expressionEvaluator, 
                           ITokenReader tokenReader, 
                           IJsonTokenReader jsonTokenReader,
                           IQueryPathProvider queryPathProvider,
                           IMethodReferenceResolver referenceResolver,
                           object? methodContext = default)
        {
            JsonTransformer = jsonTransformer;
            ExpressionParser = expressionParser;
            ExpressionEvaluator = expressionEvaluator;
            TokenReader = tokenReader;
            JsonTokenReader = jsonTokenReader;
            QueryPathProvider = queryPathProvider;
            ReferenceResolver = referenceResolver;
            MethodContext = methodContext;
        }

        public IJsonContext RegisterMethod(MethodRegistration method)
        {
            if (method is null)
            {
                return this;
            }

            var registrations = MethodRegistrations.ToList();

            registrations.Add(method);

            MethodRegistrations = registrations.ToArray();

            return this;
        }

        public IJsonContext RegisterAllMethods(IEnumerable<MethodRegistration> methods)
        {
            if (methods is null)
            {
                return this;
            }

            var registrations = MethodRegistrations.ToList();

            registrations.AddRange(methods);

            MethodRegistrations = registrations.ToArray();

            return this;
        }

        public IJsonContext UseTransformer(string jsonTransformer)
        {
            JsonTransformer = jsonTransformer;

            return this;
        }

        public IJsonContext RegisterAllMethodsFrom<T>()
        {
            var type = typeof(T);
            var registrations = MethodRegistrations.ToList();

            var methods = from method in type.GetMethods(BindingFlags.Public)
                          let attribute = method.GetCustomAttribute<JoltExternalMethodAttribute>()
                          where attribute != null
                          select method.IsStatic ? new MethodRegistration(type.AssemblyQualifiedName, method.Name) : new MethodRegistration(method.Name, attribute.Name);

            registrations.AddRange(methods);

            MethodRegistrations = registrations.ToArray();

            return this;
        }
    }
}
