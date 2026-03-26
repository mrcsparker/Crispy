using System;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class BreakExpression : NodeExpression
    {
        public BreakExpression(NodeExpression? expr)
        {
            _ = expr;
        }

        protected internal override Expression Eval(Context scope)
        {
            var loopscope = FindFirstLoop(scope) ??
                throw new InvalidOperationException("Call to Break not inside loop.");
            return Expression.Break(loopscope.LoopBreak ??
                throw new InvalidOperationException("Loop break label was not initialized."),
                Expression.Constant(null, typeof(object)), typeof(object));
        }

        private static Context? FindFirstLoop(Context scope)
        {
            var curscope = scope;
            while (curscope != null)
            {
                if (curscope.IsLoop)
                {
                    return curscope;
                }

                curscope = curscope.Parent;
            }
            return null;
        }
    }
}
