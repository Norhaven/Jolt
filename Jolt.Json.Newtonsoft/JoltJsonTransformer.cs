using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Parsing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
        /// <param name="options">Options for controlling or enhancing the behavior of the transformer.</param>
        /// <returns>An instance of <see cref="IJsonTransformer{TContext}"/>.</returns>
        public static IJsonTransformer<IJsonContext> DefaultWith(string jsonTransformer, object? methodContext, IEnumerable<MethodRegistration>? methodRegistrations = default, JoltOptions? options = default)
        {
            return CreateTransformerWith(jsonTransformer, methodContext, methodRegistrations, options);
        }

        /// <summary>
        /// Creates a default Jolt transformer that will use the provided transformer and any external methods defined
        /// within the instance of <typeparamref name="TMethodContext"/>. All methods to register using this method must
        /// be annotated with the <see cref="JoltExternalMethodAttribute"/> attribute.
        /// </summary>
        /// <typeparam name="TMethodContext">The method context type for any instance or static method registrations.</typeparam>
        /// <param name="jsonTransformer">The JSON transformer to use.</param>
        /// <param name="methodContext">The method context for any instance method registrations.</param>
        /// <param name="options">Options for controlling or enhancing the behavior of the transformer.</param>
        /// <returns>An instance of <see cref="IJsonTransformer{TContext}"/>.</returns>
        public static IJsonTransformer<IJsonContext> DefaultWith<TMethodContext>(string jsonTransformer, TMethodContext? methodContext = default, JoltOptions? options = default)
            where TMethodContext : class
        {
            var thirdPartyMethods = GetExternalMethodRegistrationsFrom<TMethodContext>();

            return CreateTransformerWith(jsonTransformer, methodContext, thirdPartyMethods, options);
        }

        private static JoltJsonTransformer CreateTransformerWith(string jsonTransformer, object? methodContext, IEnumerable<MethodRegistration>? methodRegistrations, JoltOptions? options)
        {
            options ??= JoltOptions.Default;
            methodRegistrations ??= Enumerable.Empty<MethodRegistration>();

            var messageProvider = new MessageProvider(options);

            var context = new JoltContext(
                jsonTransformer,
                new ExpressionParser(),
                new ExpressionEvaluator(options),
                new TokenReader(messageProvider),
                new JsonTokenReader(),
                new JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(options),
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
            // We're setting the default settings for JsonConvert at a global level here because we'd
            // like to have these custom converters, contract resolver, and date handling to be used for
            // any JSON deserialization that occurs (until such time as we can resolve this through a
            // more configurable approach like dependency injection via the context). We're specifically
            // setting the DateParseHandling to None because otherwise Newtonsoft will parse any date-like strings
            // into a JToken with a Date type and 1) We don't have that concept at the moment, and 2) We'll lose
            // the formatting of the original string and cause things like ParseExact to choke on the resulting value.

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters =
                {
                    new JoltJsonObjectConverter(),
                    new JoltJsonArrayConverter()
                },
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.None
            };
        }
    }
}
