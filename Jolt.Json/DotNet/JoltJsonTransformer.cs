using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.DotNet
{
    public class JoltJsonTransformer : JoltTransformer<JoltContext>
    {
        public static JoltJsonTransformer DefaultWith(string jsonTransformer, IEnumerable<MethodRegistration>? methodRegistrations = null)
        {
            var context = new JoltContext(
                jsonTransformer,
                new ExpressionParser(),
                new ExpressionEvaluator(),
                new TokenReader(),
                new JsonTokenReader(),
                new IndexedPathQueryPathProvider(),
                methodRegistrations);

            return new JoltJsonTransformer(context);
        }

        public JoltJsonTransformer(JoltContext context) 
            : base(context)
        {
        }
    }
}
