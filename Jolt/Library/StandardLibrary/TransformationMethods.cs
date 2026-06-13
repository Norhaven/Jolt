using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal sealed class TransformationMethods
    {
        [JoltLibraryMethod("transformWith")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Transform(object? content, object? transformerName, [OptionalParameter(default)] IJsonObject parameters, EvaluationContext context)
        {
            var source = (IJsonToken)context.ResolveValueOf<object?>(content);
            var name = (string)context.ResolveValueOf<string>(transformerName);
            var transformer = context.JsonContext.TransformerRegistrations.FirstOrDefault(x => x.TransformerName == name);

            if (transformer is null)
            {
                throw context.CreateExecutionErrorFor<TransformationMethods>(ExceptionCode.UnableToLocateReferencedTransformer, name);
            }

            var jsonTransformer = context.JsonContext.JsonTokenReader.Read(transformer.Transformer);

            var transformationToken = EvaluationToken.From(jsonTransformer);
            var newScope = EvaluationScope.Empty
                .AddOrUpdateVariable(new RangeVariable("@params", parameters), forceApplyToCurrentLayer: true)
                .CreateClosureOver(source);

            return context.Transform(transformationToken, newScope);
        }
    }
}
