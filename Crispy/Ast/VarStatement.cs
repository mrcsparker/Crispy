
using System.Linq.Expressions;

namespace Crispy.Ast
{
    class VarStatement : NodeExpression
    {
        private readonly string _name;
        private readonly NodeExpression _value;
        private static readonly DefaultExpression VoidInstance = Expression.Empty();

        public VarStatement(string name, NodeExpression value)
        {
            _name = name;
            _value = value;
        }

        protected internal override Expression Eval(Context scope)
        {
            var variable = scope.GetOrMakeLocal(_name);

            if (_value != null)
            {
                return Expression.Assign(variable, Expression.Convert(_value.Eval(scope), variable.Type));
            }

            return VoidInstance;
        }

        public override string Name {
            get { return _name; }
        }

        public NodeExpression Value
        {
            get { return _value; }
        }
    }
}
