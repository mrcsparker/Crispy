using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class LambdaExpression : NodeExpression
    {
        private readonly CallableParameter[] _parameters;
        private readonly NodeExpression _body;

        public LambdaExpression(CallableParameter[] parameters, NodeExpression body)
        {
            _parameters = parameters;
            _body = body;
        }

        protected internal override Expression Eval(Context scope)
        {
            return CallableExpressionBuilder.BuildCallableExpression(scope, "lambda", _parameters, _body);
        }
    }
}
