using System.Linq.Expressions;

namespace Crispy.Ast
{
    class ParentesizedExpression : NodeExpression
    {
        private readonly NodeExpression _expression;

        public ParentesizedExpression(NodeExpression expression)
        {
            _expression = expression;
        }

        protected internal override Expression Eval(Context scope)
        {
            return _expression.Eval(scope);
        }
    }
}
