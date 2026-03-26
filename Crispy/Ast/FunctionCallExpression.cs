using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System;
using Crispy.Binders;
using Crispy.Helpers;

namespace Crispy.Ast
{
    internal sealed class FunctionCallExpression : NodeExpression
    {
        public IList<NodeExpression> ArgumentList { get; }
        public NodeExpression MethodName { get; }


        public FunctionCallExpression(NodeExpression methodName, IList<NodeExpression> argumentList)
        {
            MethodName = methodName;
            ArgumentList = argumentList;
        }

        protected internal override Expression Eval(Context context)
        {
            var instanceObjectCall = TryInstanceObjects(context);
            if (instanceObjectCall != null)
            {
                return RuntimeHelpers.EnsureObjectResult(instanceObjectCall);
            }

            var fun = MethodName.Eval(context);
            var args = new List<Expression> { fun };
            args.AddRange(ArgumentList.Select(a => a.Eval(context)));
            return MethodName.IsMember
                ? Expression.Dynamic(
                    // Dotted exprs must be simple invoke members, a.b.(c ...)
                    context.Runtime.GetInvokeMemberBinder(
                        new InvokeMemberBinderKey(
                            MethodName.Name,
                            new CallInfo(ArgumentList.Count))),
                    typeof(object),
                    args
                )
                : Expression.Dynamic(
                    context.Runtime
                         .GetInvokeBinder(new CallInfo(ArgumentList.Count)),
                    typeof(object),
                    args
                );

        }

        /// <summary>
        /// Kind of a bleh way of looking up a method, but this is still in progress.
        /// 
        /// Before I use the InvokeBinder, this has to be correct - every time.
        /// The InvokeBinder works on functions created in the system, this works
        /// on methods from a set of injected InstanceObjects.
        /// 
        /// I am not sure if it belongs there, or in InvokeMemberBinder since 
        /// it actually a member of an object.
        /// 
        /// In a perfect world, everything would just kind of work the same way.
        /// 
        /// ... and we would be using lisp
        /// 
        /// ... and we would be using fast decimal types instead of IEEE754 floats
        /// 
        /// ... and PI would equal 3
        ///
        /// Well, maybe not that last one.
        /// 
        /// </summary>
        /// <returns>The instance objects.</returns>
        /// <param name="context">Context.</param>
        private MethodCallExpression? TryInstanceObjects(Context context)
        {
            var argumentExpressions = new Expression[ArgumentList.Count];
            var argumentTypes = new Type[ArgumentList.Count];
            for (int i = 0; i < ArgumentList.Count; i++)
            {
                argumentExpressions[i] = ArgumentList[i].Eval(context);
                argumentTypes[i] = GetArgumentType(ArgumentList[i], argumentExpressions[i]);
            }

            string? resolutionError = null;
            if (context.InstanceObjects != null && context.InstanceObjects.Count > 0)
            {
                var candidateObjects = GetCandidateInstanceObjects(context.InstanceObjects);
                foreach (var instanceObject in context.InstanceObjects)
                {
                    if (!candidateObjects.Contains(instanceObject))
                    {
                        continue;
                    }

                    var resolution = RuntimeHelpers.ResolveMethodOverload(
                        instanceObject.GetType()
                            .GetMethods()
                            .Where(method => method.Name.Equals(MethodName.Name, StringComparison.OrdinalIgnoreCase)),
                        argumentTypes,
                        "member '" + MethodName.Name + "'");
                    if (resolution.ErrorMessage != null)
                    {
                        resolutionError ??= resolution.ErrorMessage;
                        continue;
                    }

                    if (resolution.Method != null)
                    {
                        var instanceFunctionCall = resolution.Method;
                        var parameters = instanceFunctionCall.GetParameters();
                        var convertedArguments = new Expression[argumentExpressions.Length];
                        for (int i = 0; i < argumentExpressions.Length; i++)
                        {
                            convertedArguments[i] = ConvertArgument(
                                argumentExpressions[i],
                                argumentTypes[i],
                                parameters[i].ParameterType);
                        }

                        var instanceExpression = instanceFunctionCall.IsStatic
                            ? null
                            : Expression.Constant(instanceObject);
                        MethodCallExpression expr = Expression.Call(instanceExpression,
                            instanceFunctionCall,
                            convertedArguments);
                        return expr;
                    }
                }
            }

            return resolutionError != null
                ? throw new InvalidOperationException(resolutionError)
                : null;
        }

        private IEnumerable<object> GetCandidateInstanceObjects(IEnumerable<object> instanceObjects)
        {
            return MethodName is MemberExpression memberExpression &&
                memberExpression.Expr is NamedExpression namedExpression
                ? instanceObjects.Where(instanceObject =>
                    string.Equals(
                        instanceObject.GetType().Name,
                        namedExpression.Name,
                        StringComparison.OrdinalIgnoreCase))
                : instanceObjects;
        }

        private static Type GetArgumentType(NodeExpression argument, Expression expression)
        {
            return argument is ConstantExpression constantExpression && constantExpression.Value != null
                ? constantExpression.Value.GetType()
                : expression.Type;
        }

        private static Expression ConvertArgument(Expression expression, Type argumentType, Type parameterType)
        {
            if (parameterType == typeof(object) && expression.Type == typeof(object))
            {
                return expression;
            }

            var convertedExpression = expression;
            if (argumentType != null && convertedExpression.Type != argumentType)
            {
                convertedExpression = Expression.Convert(convertedExpression, argumentType);
            }

            if (convertedExpression.Type != parameterType)
            {
                convertedExpression = Expression.Convert(convertedExpression, parameterType);
            }

            return convertedExpression;
        }

    }
}
