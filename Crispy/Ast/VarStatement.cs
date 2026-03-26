
using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class VarStatement : NodeExpression
    {
        private static readonly DefaultExpression VoidInstance = Expression.Empty();

        public override string Name { get; }
        public NodeExpression? Value { get; }

        public VarStatement(string name, NodeExpression? value)
        {
            Name = name;
            Value = value;
        }

        protected internal override Expression Eval(Context scope)
        {
            var variable = scope.GetOrMakeLocal(Name);
            return Value != null
                ? Expression.Assign(variable, Expression.Convert(Value.Eval(scope), variable.Type))
                : VoidInstance;
        }
    }
}
