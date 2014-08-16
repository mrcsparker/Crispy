using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    class FunctionDefStatement : NodeExpression
    {
        private readonly string _name;
        private readonly string[] _parameters;
        private readonly NodeExpression _body;

        public FunctionDefStatement(string name, string[] parameters, NodeExpression body)
        {
            _name = name;
            _parameters = parameters;
            _body = body;
        }

        // We are going to have fun here.  All functions are lambda functions by default,
        // which means that this will be easy to extend in the future.
        //
        // The function looks like:
        //
        // object functionName(scope.ModuleExpr,
        //   lambda (param0, param1) {
        //   })
        //
        protected internal override Expression Eval(Context scope)
        {
            if (!scope.IsModule)
            {
                throw new InvalidOperationException(
                    "Can't create functions inside of functions yet.");
            }

            // Create new scope for this function
            var functionScope = new Context(scope, "function " + _name) {
                IsLambda = true
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

            var lambdaExpression =
                Expression.Lambda(
                    Expression.GetFuncType(funcTypeArgs.ToArray()),
                    MakeBody(functionScope), 
                    paramsInOrder
                );

            return Expression.Dynamic(
                scope.GetRuntime().GetSetMemberBinder(_name),
                typeof(object), 
                scope.ModuleExpr, 
                lambdaExpression
            );
        }

        private Expression MakeBody(Context context)
        {
            Expression body = _body.Eval(context);
            LabelTarget returnLabel = context.GetReturnLabel();

            if (context.Variables.Count > 0)
            {
                if (returnLabel != null)
                {
                    return Expression.Block(
                        context.Variables.Select(name => name.Value).ToArray(),
                        body,
                        Expression.Label(context.GetReturnLabel(), Expression.Constant(null))
                    );
                }
                return Expression.Block(
                    context.Variables.Select(name => name.Value).ToArray(),
                    body);

            }


            if (returnLabel != null)
            {
                return Expression.Block(
                    body, 
                    Expression.Label(context.GetReturnLabel(), Expression.Constant(null))
                );
            }
            return body;
        }

        public override string Name {
            get { return _name; }
        }
    }
}
