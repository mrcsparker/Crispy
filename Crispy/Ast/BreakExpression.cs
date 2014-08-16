using System;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    class BreakExpression : NodeExpression
    {
        NodeExpression _expr;

        public BreakExpression(NodeExpression expr)
        {
            _expr = expr;
        }

        protected internal override Expression Eval(Context scope)
        {
            var loopscope = _findFirstLoop(scope);
            if (loopscope == null)
                throw new InvalidOperationException(
                               "Call to Break not inside loop.");
            return Expression.Break(loopscope.LoopBreak, 
                Expression.Constant(null, typeof(object)), typeof(object));
        }

        private static Context _findFirstLoop(Context scope)
        {
            var curscope = scope;
            while (curscope != null)
            {
                if (curscope.IsLoop)
                    return curscope;
                curscope = curscope.Parent;
            }
            return null;
        }
    }
}
