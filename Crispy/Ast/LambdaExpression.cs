using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Crispy.Ast
{
    class LambdaExpression : NodeExpression
    {
        private readonly string[] _parameters;
        private readonly NodeExpression _body;

        public LambdaExpression(string[] parameters, NodeExpression body)
        {
            _parameters = parameters;
            _body = body;
        }

        protected internal override Expression Eval(Context scope)
        {
            // Create new scope for this function
            var functionScope = new Context(scope, "lambda") {
                IsLambda = true,
                ReturnLabel = Expression.Label(typeof(object))
            };

            // We are now going to grab the function parameters and add them to a list of
            // variables accessible in the function
            var paramsInOrder = new List<ParameterExpression>();
            foreach (var p in _parameters)
            {
                var pe = Expression.Parameter(typeof(object), p);
                paramsInOrder.Add(pe);
                functionScope.Params[p.ToLower()] = pe;
            }

            // We add an extra object type to the args.
            // An extra type is the return value
            var funcTypeArgs = new List<Type>();
            for (var i = 0; i < _parameters.Length + 1; i++)
            {
                funcTypeArgs.Add(typeof(object));
            }

            return Expression.Lambda(
                Expression.GetFuncType(funcTypeArgs.ToArray()),
                MakeBody(functionScope), 
                paramsInOrder
            );
        }

        private Expression MakeBody(Context context)
        {
            Expression body = _body.Eval(context);

            if (context.Variables.Count > 0)
            {
                return Expression.Block(
                    context.Variables.Select(name => name.Value).ToArray(),
                    body,
                    Expression.Label(context.GetReturnLabel(), Expression.Constant(null))
                );
            }

            return Expression.Block(body, Expression.Label(context.GetReturnLabel(), Expression.Constant(null)));
        }
    }
}

