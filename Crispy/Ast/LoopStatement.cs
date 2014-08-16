using System.Linq.Expressions;

namespace Crispy.Ast
{
    class LoopStatement : NodeExpression
    {
        private readonly NodeExpression _body;

        public LoopStatement(NodeExpression body)
        {
            _body = body;
        }

        protected internal override Expression Eval(Context scope)
        {
            var loopscope = new Context(scope, "loop")
                {
                    IsLoop = true,
                    LoopBreak = Expression.Label(typeof (object), "loop break")
                };

            return Expression.Loop(Expression.Block(typeof(object), _body.Eval(loopscope)),
                                   loopscope.LoopBreak);

        }
    }
}
