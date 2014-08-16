using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Crispy.Binders;
using System.Reflection;
using System;
using System.Globalization;
using Crispy.Helpers;

namespace Crispy.Ast
{
    internal class FunctionCallExpression : NodeExpression
    {
        private readonly IList<NodeExpression> _argumentList;
        private readonly NodeExpression _methodName;


        public FunctionCallExpression(NodeExpression methodName, IList<NodeExpression> argumentList)
        {
            _methodName = methodName;
            _argumentList = argumentList;
        }

        protected internal override Expression Eval(Context context)
        {
            var fun = _methodName.Eval(context);
            var args = new List<Expression> {fun};
            args.AddRange(_argumentList.Select(a => a.Eval(context)));

            if (_methodName.IsMember)
            {
                return Expression.Dynamic(
                    // Dotted exprs must be simple invoke members, a.b.(c ...) 
                    context.GetRuntime().GetInvokeMemberBinder(
                        new InvokeMemberBinderKey(
                            _methodName.Name,
                            new CallInfo(_argumentList.ToArray().Length))),
                    typeof(object),
                    args
                );
            }

            return Expression.Dynamic(
                context.GetRuntime()
                     .GetInvokeBinder(new CallInfo(_argumentList.ToArray().Length)),
                typeof (object),
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
        protected Expression TryInstanceObjects(Context context)
        {

            var argumentExpressions = new Expression[_argumentList.Count];
            var argumentTypes = new Type[_argumentList.Count];
            for (int i = 0; i < _argumentList.Count; i++)
            {
                argumentExpressions[i] = _argumentList[i].Eval(context);
                argumentTypes[i] = argumentExpressions[i].Type;
            }

            if (context.InstanceObjects != null && context.InstanceObjects.Count > 0)
            {
                foreach (var instanceObject in context.InstanceObjects)
                {
                    MethodInfo instanceFunctionCall = FindMethod(instanceObject.GetType(), _methodName.Name,
                        argumentTypes);
                    if (instanceFunctionCall != null)
                    {
                        MethodCallExpression expr = Expression.Call(Expression.Constant(instanceObject),
                            instanceFunctionCall,
                            argumentExpressions);
                        return expr;
                    }
                }
            }
            return null;
        }

        public static MethodInfo FindMethod(Type type, string methodName, Type[] argumentTypes)
        {
            // Most likely we are passing in something like ABS(1) which maps to
            // Math.Abs(1).  GetMethod() is case sensitive which is why we have to do this.
            string tryCapitalCase = methodName.First().ToString(CultureInfo.InvariantCulture).ToUpper() +
                String.Join("", methodName.Skip(1));
            if (TryGetMethod(type, tryCapitalCase, argumentTypes) != null)
            {
                return TryGetMethod(type, tryCapitalCase, argumentTypes);
            }

            // This probably won't return a match unless people are 
            // explicitly typing in Abs(1).  Since this is mostly an
            // expression language users will be used to standard Excel Formula
            // uppercase-everything
            if (TryGetMethod(type, methodName, argumentTypes) != null)
            {
                return TryGetMethod(type, methodName, argumentTypes);
            }

            // Now we search for a matching method.
            // This is going to go through all of the methods and try to
            // match parameters. 
            //
            // For example, ABS(1.0) is going to be matched against double Math.Abs(double i)
            MethodInfo[] methods = type.GetMethods();
            foreach (MethodInfo method in methods)
            {

                if (method.Name.Equals(methodName, StringComparison.InvariantCultureIgnoreCase) &&
                    method.GetParameters().Length == argumentTypes.Length)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo parameter = parameters[i];
                        if (parameter.IsOut)
                        {
                            break;
                        }
                        if (!parameter.ParameterType.IsAssignableFrom(argumentTypes[i]))
                        {
                            break;
                        }

                        if (!parameter.ParameterType.IsPrimitive)
                        {
                            break;
                        }

                        if (parameter.ParameterType == typeof (char))
                        {
                            break;
                        }

                        if (i == parameters.Length - 1)
                        {
                            return method;
                        }
                    }
                }
            }
            return null;
        }

        public static MethodInfo TryGetMethod(Type type, string methodName, Type[] types)
        {
            if (!types.Any() || types.FirstOrDefault() == null)
            {
                return type.GetMethod(methodName);
            }
            return type.GetMethod(methodName, types);
        }

        public IList<NodeExpression> ArgumentList
        {
            get { return _argumentList; }
        }

        public NodeExpression MethodName
        {
            get { return _methodName; }
        }
    }
}
