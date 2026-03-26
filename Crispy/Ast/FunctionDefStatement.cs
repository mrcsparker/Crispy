using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class FunctionDefStatement : NodeExpression
    {
        private readonly string _name;
        private readonly CallableParameter[] _parameters;
        private readonly NodeExpression _body;

        public FunctionDefStatement(string name, CallableParameter[] parameters, NodeExpression body)
        {
            _name = name;
            _parameters = parameters;
            _body = body;
        }

        // We are going to have fun here.  All functions are lambda functions by default,
        // which means that this will be easy to extend in the future.
        //
        protected internal override Expression Eval(Context scope)
        {
            var local = scope.HasCallableAncestor
                ? scope.GetOrMakeLocal(_name)
                : null;
            var callableExpression =
                CallableExpressionBuilder.BuildCallableExpression(scope, "function " + _name, _parameters, _body);

            return local != null
                ? Expression.Assign(
                    local,
                    Expression.Convert(callableExpression, local.Type))
                : Expression.Dynamic(
                    scope.Runtime.GetSetMemberBinder(_name),
                    typeof(object),
                    scope.ModuleExpr,
                    callableExpression
                );
        }

        public override string Name
        {
            get { return _name; }
        }
    }
}
