using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class ForeachStatement : NodeExpression
    {
        private static readonly MethodInfo GetEnumeratorMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetEnumerator)) ??
            throw new InvalidOperationException("RuntimeHelpers.GetEnumerator was not found.");

        private static readonly MethodInfo MoveNextMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.MoveNext)) ??
            throw new InvalidOperationException("RuntimeHelpers.MoveNext was not found.");

        private static readonly MethodInfo GetCurrentMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetCurrent)) ??
            throw new InvalidOperationException("RuntimeHelpers.GetCurrent was not found.");

        private static readonly MethodInfo DisposeEnumeratorMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.DisposeEnumerator)) ??
            throw new InvalidOperationException("RuntimeHelpers.DisposeEnumerator was not found.");

        private readonly string _itemName;
        private readonly NodeExpression _sequence;
        private readonly NodeExpression _body;

        public ForeachStatement(string itemName, NodeExpression sequence, NodeExpression body)
        {
            _itemName = itemName;
            _sequence = sequence;
            _body = body;
        }

        protected internal override Expression Eval(Context scope)
        {
            var breakLabel = Expression.Label(typeof(object), "foreach break");
            var continueLabel = Expression.Label("foreach continue");
            var enumerator = Expression.Variable(typeof(IEnumerator), "__enumerator");

            var loopScope = new Context(scope, "foreach")
            {
                IsLoop = true,
                LoopBreak = breakLabel,
                LoopContinue = continueLabel
            };

            var iterationScope = new Context(loopScope, "foreach iteration");
            var itemVariable = iterationScope.GetOrMakeLocal(_itemName);
            var body = _body.Eval(iterationScope);

            var iterationExpressions = new Expression[2];
            iterationExpressions[0] = Expression.Assign(
                itemVariable,
                Expression.Convert(
                    Expression.Call(GetCurrentMethod, enumerator),
                    itemVariable.Type));
            iterationExpressions[1] = body;

            var iterationBlock = Expression.Block(
                typeof(object),
                [.. iterationScope.Variables.Values],
                iterationExpressions);

            var loopBody = Expression.Condition(
                Expression.Call(MoveNextMethod, enumerator),
                iterationBlock,
                Expression.Break(
                    breakLabel,
                    Expression.Constant(null, typeof(object)),
                    typeof(object)));

            return Expression.Block(
                typeof(object),
                [enumerator],
                Expression.Assign(
                    enumerator,
                    Expression.Call(
                        GetEnumeratorMethod,
                        Expression.Convert(_sequence.Eval(scope), typeof(object)))),
                Expression.TryFinally(
                    Expression.Loop(loopBody, breakLabel, continueLabel),
                    Expression.Call(DisposeEnumeratorMethod, enumerator)));
        }
    }
}
