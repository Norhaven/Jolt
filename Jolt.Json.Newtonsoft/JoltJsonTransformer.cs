using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    /// <summary>
    /// Represents a JSON transformer capable of reading and executing Jolt transformations.
    /// </summary>
    public sealed class JoltJsonTransformer : JoltTransformer<JoltContext>
    {
        /// <summary>
        /// Creates a default Jolt transformer that will use the provided transformer and external methods.
        /// </summary>
        /// <param name="jsonTransformer">The JSON transformer to use.</param>
        /// <param name="methodContext">The method context for any instance method registrations.</param>
        /// <param name="methodRegistrations">The external method registrations to include in this transformer.</param>
        /// <returns>An instance of <see cref="IJsonTransformer{TContext}"/>.</returns>
        public static IJsonTransformer<IJsonContext> DefaultWith(string jsonTransformer, object? methodContext, IEnumerable<MethodRegistration>? methodRegistrations = default)
        {
            return CreateTransformerWith(jsonTransformer, methodContext, methodRegistrations);
        }

        /// <summary>
        /// Creates a default Jolt transformer that will use the provided transformer and any external methods defined
        /// within the instance of <typeparamref name="TMethodContext"/>. All methods to register using this method must
        /// be annotated with the <see cref="JoltExternalMethodAttribute"/> attribute.
        /// </summary>
        /// <typeparam name="TMethodContext">The method context type for any instance or static method registrations.</typeparam>
        /// <param name="jsonTransformer">The JSON transformer to use.</param>
        /// <param name="methodContext">The method context for any instance method registrations.</param>
        /// <returns>An instance of <see cref="IJsonTransformer{TContext}"/>.</returns>
        public static IJsonTransformer<IJsonContext> DefaultWith<TMethodContext>(string jsonTransformer, TMethodContext methodContext = default)
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

        /// <summary>
        /// Initializes an instance of <see cref="JoltJsonTransformer"/> with the provided context.
        /// </summary>
        /// <param name="context">The context that all transformations will occur within.</param>
        public JoltJsonTransformer(JoltContext context) 
            : base(context)
        {
        }
    }
}
