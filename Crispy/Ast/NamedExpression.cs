using System.Linq.Expressions;

namespace Crispy.Ast
{
    class NamedExpression : NodeExpression
    {
        private readonly string _name;

        public NamedExpression(string name)
        {
            _name = name;
        }

        protected internal override Expression Eval(Context scope)
        {
            var variable = scope.LookupName(_name);
            if (variable != null)
            {
                return variable;
            }

            return Expression.Dynamic(
                scope.GetRuntime().GetGetMemberBinder(_name),
                typeof(object),
                scope.GetModuleExpr()
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

        public override string Name {
            get { return _name; }
        }

    }
}
