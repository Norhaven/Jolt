using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JoltJsonTransformer : JoltTransformer<JoltContext>
    {
        public static JoltJsonTransformer DefaultWith(string jsonTransformer, object? methodContext, IEnumerable<MethodRegistration>? methodRegistrations = null)
        {
            return CreateTransformerWith(jsonTransformer, methodContext, methodRegistrations);
        }

        public static JoltJsonTransformer DefaultWith<TMethodContext>(string jsonTransformer, TMethodContext methodContext = default)
        {
            var thirdPartyMethods = GetExternalMethodRegistrationsFrom<TMethodContext>();

            return CreateTransformerWith(jsonTransformer, methodContext, thirdPartyMethods);
        }

        private static JoltJsonTransformer CreateTransformerWith(string jsonTransformer, object? methodContext, IEnumerable<MethodRegistration> methodRegistrations)
        {
            var context = new JoltContext(
                jsonTransformer,
                new ExpressionParser(),
                new ExpressionEvaluator(),
                new TokenReader(),
                new JsonTokenReader(),
                new JsonPathQueryPathProvider(),
                new MethodReferenceResolver(),
                methodContext);

            context.RegisterAllMethods(methodRegistrations);

            return new JoltJsonTransformer(context);
        }

        public JoltJsonTransformer(JoltContext context) 
            : base(context)
        {
        }
    }
}
