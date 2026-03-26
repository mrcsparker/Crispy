using System;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class ContinueExpression : NodeExpression
    {
        protected internal override Expression Eval(Context scope)
        {
            var loopScope = FindFirstLoop(scope) ??
                throw new InvalidOperationException("Call to Continue not inside loop.");
            var continueLabel = loopScope.LoopContinue ??
                throw new InvalidOperationException("Loop continue label was not initialized.");

            return Expression.Block(
                Expression.Continue(continueLabel),
                Expression.Constant(null, typeof(object)));
        }

        private static Context? FindFirstLoop(Context scope)
        {
            var curScope = scope;
            while (curScope != null)
            {
                if (curScope.IsLoop)
                {
                    return curScope;
                }

                curScope = curScope.Parent;
            }

            return null;
        }
    }
}
