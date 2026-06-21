using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Resources.TestMethods;
using Jolt.Testing.Transformers;
using Jolt.Testing.Transformers.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Jolt.Testing.Resources.Extensions;
using Jolt.Testing.Resources;

namespace Jolt.Testing.Harness.DotNet
{
    public sealed class TransformationTests : Testing.TransformationTests
    {
        [TransformerTest(Transformer.PartialTransformerReference, SourceDocument.PartialReferenceDocument, typeof(TestContext), TestType.DotNet)]
        public override Task PartialTransformerReference_WithExecutionTrace_IsSuccessful(TransformerTest test)
        {
            return base.PartialTransformerReference_WithExecutionTrace_IsSuccessful(test);
        }

        [TransformerTest(Transformer.StreamingTransformer, SourceDocument.StreamingDocument, typeof(TestContext), TestType.DotNet)]
        public override Task ReaderWriterTransformations_AreSuccessful(TransformerTest test)
        {
            return base.ReaderWriterTransformations_AreSuccessful(test);
        }

        [TransformerTest(Transformer.StreamingTransformer, SourceDocument.StreamingDocument, typeof(TestContext), TestType.DotNet)]
        public override Task StreamingTransformations_AreSuccessful(TransformerTest test)
        {
            return base.StreamingTransformations_AreSuccessful(test);
        }

        [TransformerTest(Transformer.PartialTransformerReference, SourceDocument.PartialReferenceDocument, typeof(TestContext), TestType.DotNet)]
        public override Task PartialTransformerReference_IsSuccessful(TransformerTest test)
        {
            return base.PartialTransformerReference_IsSuccessful(test);
        }

        [TransformerTest(Transformer.ArrayLiterals, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ArrayLiteral_IsSuccessful_AtSingleLevel(TransformerTest test)
        {
            return base.ArrayLiteral_IsSuccessful_AtSingleLevel(test);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ValueOf_IsSuccessful_AtSingleLevelForNumericLiteral(TransformerTest test)
        {
            return base.ValueOf_IsSuccessful_AtSingleLevelForNumericLiteral(test);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ValueOf_IsSuccessful_AtSingleLevelForStringLiteral(TransformerTest test)
        {
            return base.ValueOf_IsSuccessful_AtSingleLevelForStringLiteral(test);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ValueOf_IsSuccessful_AtSingleLevelForBooleanTrueLiteral(TransformerTest test)
        {
            return base.ValueOf_IsSuccessful_AtSingleLevelForBooleanTrueLiteral(test);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ValueOf_IsSuccessful_AtSingleLevelForBooleanFalseLiteral(TransformerTest test)
        {
            return base.ValueOf_IsSuccessful_AtSingleLevelForBooleanFalseLiteral(test);
        }

        [TransformerTest(Transformer.MultiLevelValueOf, SourceDocument.MultiLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ValueOf_IsSuccessful_AtMultiLevelForStringLiteral(TransformerTest test)
        {
            return base.ValueOf_IsSuccessful_AtMultiLevelForStringLiteral(test);
        }

        [TransformerTest(Transformer.Loops, SourceDocument.Loop, typeof(TestContext), TestType.DotNet)]
        public override Task Loop_IsSuccessful_AtSingleLevelForMultiElements(TransformerTest test)
        {
            return base.Loop_IsSuccessful_AtSingleLevelForMultiElements(test);
        }

        [TransformerTest(Transformer.MultiLevelValueOf, SourceDocument.MultiLevel, typeof(TestContext), TestType.DotNet)]
        public override Task ValueOf_WorksSuccessfullyAtMultipleLevels(TransformerTest test)
        {
            return base.ValueOf_WorksSuccessfullyAtMultipleLevels(test);
        }

        [TransformerTest(Transformer.Math, SourceDocument.Math, typeof(TestContext), TestType.DotNet)]
        public override Task Math_IsSuccessful_WithOperatorPrecedence(TransformerTest test)
        {
            return base.Math_IsSuccessful_WithOperatorPrecedence(test);
        }

        [TransformerTest(Transformer.Existence, SourceDocument.Existence, typeof(TestContext), TestType.DotNet)]
        public override Task Existence_IsSuccessful_WithLiteralStringAndNull(TransformerTest test)
        {
            return base.Existence_IsSuccessful_WithLiteralStringAndNull(test);
        }

        [TransformerTest(Transformer.Conditions, SourceDocument.Conditions, typeof(TestContext), TestType.DotNet)]
        public override Task IfCondition_IsSuccessful_WithStringComparisonAsConditionAndStringResult(TransformerTest test)
        {
            return base.IfCondition_IsSuccessful_WithStringComparisonAsConditionAndStringResult(test);
        }

        [TransformerTest(Transformer.PipedMethods, SourceDocument.PipedMethods, typeof(TestContext), TestType.DotNet)]
        public override Task PipedMethod_IsSuccessful_WithCallToSingleAndMultiParameterMethod(TransformerTest test)
        {
            return base.PipedMethod_IsSuccessful_WithCallToSingleAndMultiParameterMethod(test);
        }

        [TransformerTest(Transformer.ExternalMethods, SourceDocument.ExternalMethods, typeof(TestContext), TestType.DotNet, typeof(ExternalInstanceMethods))]
        public override Task ExternalMethod_IsSuccessful_WithStaticAndInstanceCallToSingleAndMultiParameterMethod(TransformerTest test)
        {
            return base.ExternalMethod_IsSuccessful_WithStaticAndInstanceCallToSingleAndMultiParameterMethod(test);
        }

        [TransformerTest("ExternalMethodsWithAliases", "ExternalMethodsDocument", typeof(TestContext), TestType.DotNet, typeof(ExternalMixedMethods))]
        public override Task ExternalMethod_IsSuccessful_UsingDefaultWithStaticMethodAliasedRegistrations(TransformerTest test)
        {
            return base.ExternalMethod_IsSuccessful_UsingDefaultWithStaticMethodAliasedRegistrations(test);
        }

        [TransformerTest(Transformer.RangeVariables, SourceDocument.Loop, typeof(TestContext), TestType.DotNet)]
        public override Task RangeVariables_AreSuccessful_WithDeclarationAndUsage(TransformerTest test)
        {
            return base.RangeVariables_AreSuccessful_WithDeclarationAndUsage(test);
        }

        [TransformerTest(Transformer.Lambdas, SourceDocument.Lambdas, typeof(TestContext), TestType.DotNet)]
        public override Task Lambdas_AreSuccessful_WithDeclarationAndUsage(TransformerTest test)
        {
            return base.Lambdas_AreSuccessful_WithDeclarationAndUsage(test);
        }

        [TransformerTest(Transformer.UsingBlock, SourceDocument.Loop, typeof(TestContext), TestType.DotNet)]
        public override Task UsingBlock_IsSuccessful_WithDeclarationAndUsageIncludingFromRangeVariables(TransformerTest test)
        {
            return base.UsingBlock_IsSuccessful_WithDeclarationAndUsageIncludingFromRangeVariables(test);
        }
    }
}
