using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public class JoltJsonTransformer : JoltTransformer<JoltContext>
    {
        public static JoltJsonTransformer DefaultWith(string jsonTransformer, IEnumerable<MethodRegistration>? methodRegistrations = null)
        {
            var referenceResolver = new MethodReferenceResolver(methodRegistrations);

            return CreateTransformerWith(jsonTransformer, referenceResolver);
        }

        public static JoltJsonTransformer DefaultWith<TMethodContext>(string jsonTransformer)
        {
            var thirdPartyMethods = GetExternalMethodRegistrationsFrom<TMethodContext>();
            var referenceResolver = new MethodReferenceResolver(thirdPartyMethods);

            return CreateTransformerWith(jsonTransformer, referenceResolver);
        }

        private static JoltJsonTransformer CreateTransformerWith(string jsonTransformer, IMethodReferenceResolver referenceResolver)
        {
            var context = new JoltContext(
                jsonTransformer,
                new ExpressionParser(),
                new ExpressionEvaluator(),
                new TokenReader(),
                new JsonTokenReader(),
                new JsonPathQueryPathProvider(),
                referenceResolver);

            return new JoltJsonTransformer(context);
        }

        public JoltJsonTransformer(JoltContext context) 
            : base(context)
        {
        }
    }
}
