using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class LoopStatement : NodeExpression
    {
        private readonly NodeExpression _body;

        public LoopStatement(NodeExpression body)
        {
            _body = body;
        }

        protected internal override Expression Eval(Context scope)
        {
            var breakLabel = Expression.Label(typeof(object), "loop break");
            var continueLabel = Expression.Label("loop continue");
            var loopScope = new Context(scope, "loop")
            {
                IsLoop = true,
                LoopBreak = breakLabel,
                LoopContinue = continueLabel
            };
            var body = _body.Eval(loopScope);

            return loopScope.Variables.Count > 0
                ? Expression.Loop(
                    Expression.Block(typeof(object), [.. loopScope.Variables.Values], body),
                    breakLabel,
                    continueLabel)
                : Expression.Loop(
                    Expression.Block(typeof(object), body),
                    breakLabel,
                    continueLabel);

        }
    }
}
