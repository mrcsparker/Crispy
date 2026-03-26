using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class NamedExpression : NodeExpression
    {
        private readonly string _name;

        public NamedExpression(string name)
        {
            _name = name;
        }

        protected internal override Expression Eval(Context scope)
        {
            var variable = scope.LookupName(_name);
            return variable ?? Expression.Dynamic(
                scope.Runtime.GetGetMemberBinder(_name),
                typeof(object),
                scope.ModuleExpr
            );
        }

        protected internal override Expression SetVariable(Context scope, Expression right)
        {
            var variable = GetVariable(scope);

            return Expression.Assign(
                variable,
                Expression.Convert(right, variable.Type)
            );
        }

        private Expression GetVariable(Context scope)
        {
            var variable = scope.LookupName(_name) ?? scope.GetOrMakeGlobal(_name);
            return variable;
        }

        public override string Name
        {
            get { return _name; }
        }

    }
}
