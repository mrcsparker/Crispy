using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class ConstantExpression : NodeExpression
    {
        public object? Value { get; }

        public ConstantExpression(object? value)
        {
            Value = value;
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Constant(Value, typeof(object));
        }
    }
}
