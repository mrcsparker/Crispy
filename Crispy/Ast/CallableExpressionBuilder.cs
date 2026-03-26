using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    internal static class CallableExpressionBuilder
    {
        private static readonly MethodInfo CrispyCallableCreateMethod =
            typeof(CrispyCallable).GetMethod(
                nameof(CrispyCallable.Create),
                [typeof(Func<object[], object>), typeof(int), typeof(int)]) ??
            throw new InvalidOperationException("CrispyCallable.Create method was not found.");

        public static Expression BuildCallableExpression(
            Context parentScope,
            string name,
            IReadOnlyList<CallableParameter> parameters,
            NodeExpression body)
        {
            var sawDefault = false;
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].DefaultValue != null)
                {
                    sawDefault = true;
                    continue;
                }

                if (sawDefault)
                {
                    throw new InvalidOperationException("Required parameters cannot follow optional parameters.");
                }
            }

            return sawDefault
                ? BuildDefaultedCallableExpression(parentScope, name, parameters, body)
                : BuildDelegateLambda(parentScope, name, parameters, body);
        }

        private static System.Linq.Expressions.LambdaExpression BuildDelegateLambda(
            Context parentScope,
            string name,
            IReadOnlyList<CallableParameter> parameters,
            NodeExpression body)
        {
            var callableScope = new Context(parentScope, name)
            {
                IsLambda = true
            };

            var parameterExpressions = new ParameterExpression[parameters.Count];
            var delegateTypeArgs = new Type[parameters.Count + 1];
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameterName = parameters[i].Name;
                var parameterExpression = Expression.Parameter(typeof(object), parameterName);
                parameterExpressions[i] = parameterExpression;
                callableScope.Params[parameterName] = parameterExpression;
                delegateTypeArgs[i] = typeof(object);
            }

            delegateTypeArgs[^1] = typeof(object);

            return Expression.Lambda(
                Expression.GetFuncType(delegateTypeArgs),
                BuildBody(callableScope, body.Eval(callableScope)),
                parameterExpressions);
        }

        private static MethodCallExpression BuildDefaultedCallableExpression(
            Context parentScope,
            string name,
            IReadOnlyList<CallableParameter> parameters,
            NodeExpression body)
        {
            var callableScope = new Context(parentScope, name)
            {
                IsLambda = true
            };

            var arguments = Expression.Parameter(typeof(object[]), "__arguments");
            var parameterVariables = new ParameterExpression[parameters.Count];
            var parameterAssignments = new Expression[parameters.Count];
            var requiredParameterCount = GetRequiredParameterCount(parameters);

            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                var parameterVariable = Expression.Variable(typeof(object), parameter.Name);
                parameterVariables[i] = parameterVariable;
                callableScope.Params[parameter.Name] = parameterVariable;

                parameterAssignments[i] = Expression.Assign(
                    parameterVariable,
                    Expression.Condition(
                        Expression.LessThan(
                            Expression.Constant(i),
                            Expression.ArrayLength(arguments)),
                        Expression.ArrayIndex(arguments, Expression.Constant(i)),
                        parameter.DefaultValue != null
                            ? RuntimeHelpers.EnsureObjectResult(parameter.DefaultValue.Eval(callableScope))
                            : Expression.Constant(null, typeof(object))));
            }

            var implementation = Expression.Lambda<Func<object[], object>>(
                BuildBody(
                    callableScope,
                    body.Eval(callableScope),
                    parameterVariables,
                    parameterAssignments),
                arguments);

            return Expression.Call(
                CrispyCallableCreateMethod,
                implementation,
                Expression.Constant(requiredParameterCount),
                Expression.Constant(parameters.Count));
        }

        private static int GetRequiredParameterCount(IReadOnlyList<CallableParameter> parameters)
        {
            var requiredParameterCount = 0;
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].DefaultValue != null)
                {
                    return requiredParameterCount;
                }

                requiredParameterCount += 1;
            }

            return requiredParameterCount;
        }

        private static BlockExpression BuildBody(
            Context scope,
            Expression body,
            IReadOnlyList<ParameterExpression>? prefixVariables = null,
            IReadOnlyList<Expression>? prefixExpressions = null)
        {
            var variables = prefixVariables != null
                ? new List<ParameterExpression>(prefixVariables)
                : [];
            variables.AddRange(scope.Variables.Values);

            var expressions = prefixExpressions != null
                ? new List<Expression>(prefixExpressions)
                : [];
            var objectBody = RuntimeHelpers.EnsureObjectResult(body);
            var returnLabel = scope.CallableScope.ReturnLabel;
            if (returnLabel == null)
            {
                expressions.Add(objectBody);
                return variables.Count > 0
                    ? Expression.Block(variables, expressions)
                    : Expression.Block(expressions);
            }

            var result = Expression.Variable(typeof(object), "__result");
            variables.Add(result);
            expressions.Add(Expression.Assign(result, objectBody));
            expressions.Add(Expression.Label(returnLabel, result));

            return Expression.Block(variables, expressions);
        }
    }
}
