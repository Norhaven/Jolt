using Jolt.Evaluation;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library
{
    internal abstract class EnumerableWithLambda
    {
        internal sealed class QueryResult<TValue> where TValue : IJsonToken
        {
            public static QueryResult<TValue> None => new QueryResult<TValue>(default, false);
            public static QueryResult<TValue> Some(TValue result) => new QueryResult<TValue>(result, true);

            public TValue Result { get; }
            public bool HasResult { get; }

            public QueryResult(TValue result, bool hasResult)
            {
                Result = result;
                HasResult = hasResult;
            }
        }

        protected IEnumerable<IJsonToken> Sequence { get; }
        protected LambdaMethod Lambda { get; }

        public EnumerableWithLambda(IEnumerable<IJsonToken> sequence, LambdaMethod lambda)
        {
            Sequence = sequence;
            Lambda = lambda;
        }

        public QueryResult<IJsonToken> ExecuteLambdaWith(IJsonToken value, EvaluationContext context, Func<Expression, EvaluationContext, QueryResult<IJsonToken>> execute)
        {
            var itemToken = context.CreateTokenFrom(value);
            var loopVariable = new RangeVariable(Lambda.Variable.Name, itemToken);

            context.Scope.AddOrUpdateVariable(loopVariable);

            try
            {
                return execute(Lambda.Body, context);
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
        }

        protected IEnumerable<IJsonToken> Project(EvaluationContext context)
        {
            return from item in Sequence
                   let token = ExecuteLambdaWith(item, context, (body, ctx) => QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx)))
                   where token.HasResult
                   select token.Result;
        }

        protected IEnumerable<IJsonToken> ProjectWhere(EvaluationContext context)
        {
            return from item in Sequence
                   let token = ExecuteLambdaWith(item, context, (body, ctx) => ExecuteLambdaBody(body, ctx).AsValue().ToTypeOf<bool>() ? QueryResult<IJsonToken>.Some(item) : QueryResult<IJsonToken>.None)
                   where token.HasResult
                   select token.Result;
        }

        protected IEnumerable<IJsonToken> ProjectByUniqueKey(ICollection<IJsonToken> collection, EvaluationContext context)
        {
            var projectionItems = from item in Sequence
                                  let token = ExecuteLambdaWith(item, context, (body, ctx) => QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx)))
                                  where token.HasResult
                                  select (Key: token.Result, Item: item);

            foreach (var (key, item) in projectionItems)
            {
                if (!collection.Contains(key))
                {
                    collection.Add(key);
                    yield return item;
                }
            }
        }

        protected IEnumerable<IJsonToken> TakeWhile(EvaluationContext context)
        {
            foreach (var item in Sequence)
            {
                var result = ExecuteLambdaWith(item, context, (body, ctx) =>
                {
                    if (ExecuteLambdaBody(body, ctx)?.AsValue().ToTypeOf<bool>() == true)
                    {
                        return QueryResult<IJsonToken>.Some(context.CreateTokenFrom(item));
                    }

                    return QueryResult<IJsonToken>.None;
                });

                if (result.HasResult)
                {
                    yield return result.Result;
                }
                else
                {
                    yield break;
                }
            }
        }

        protected IEnumerable<IJsonToken> SkipWhile(EvaluationContext context)
        {
            var isSkipping = true;

            foreach (var item in Sequence)
            {
                var result = ExecuteLambdaWith(item, context, (body, ctx) =>
                {
                    if (isSkipping &&  ExecuteLambdaBody(body, ctx)?.AsValue().ToTypeOf<bool>() == true)
                    {
                        return QueryResult<IJsonToken>.None;
                    }

                    isSkipping = false;

                    return QueryResult<IJsonToken>.Some(context.CreateTokenFrom(item));
                });

                if (result.HasResult)
                {
                    yield return result.Result;
                }
            }
        }

        protected IJsonToken? ExecuteLambdaBody(Expression lambdaBodyExpression, EvaluationContext context)
        {
            var evaluationContext = new EvaluationContext(
                context.Mode,
                lambdaBodyExpression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform);

            var result = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

            return result.TransformedToken;
        }
    }
}
