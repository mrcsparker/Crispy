using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class ParentesizedExpression : NodeExpression
    {
        public NodeExpression Expression { get; }

        public ParentesizedExpression(NodeExpression expression)
        {
            Expression = expression;
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Eval(scope);
        }
    }
}
