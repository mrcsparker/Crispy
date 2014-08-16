using System.Linq.Expressions;

namespace Crispy.Ast
{
    class ConstantExpression : NodeExpression
    {
        private readonly object _value;

        public ConstantExpression(object value)
        {
            _value = value;
        }

        public object Value
        {
            get { return _value; }
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Constant(_value, typeof(object));
        }
    }
}
