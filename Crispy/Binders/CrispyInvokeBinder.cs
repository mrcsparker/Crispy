using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Binders
{
    /// <summary>
    /// Function call binder.  At runtime, when we are trying to call a function,
    /// a delegate will flow into the CallSite.  The default .NET meta-object will call
    /// FallbackInvoke on CrispyInvokeBinder
    /// </summary>
    /// <remarks>
    /// This is much like Ruby method_missing
    /// </remarks>
    internal sealed class CrispyInvokeBinder : InvokeBinder
    {
        private static readonly MethodInfo CrispyCallableInvokeMethod =
            typeof(CrispyCallable).GetMethod(nameof(CrispyCallable.Invoke), [typeof(object[])]) ??
            throw new InvalidOperationException("CrispyCallable.Invoke method was not found.");

        public CrispyInvokeBinder(CallInfo callinfo) : base(callinfo)
        {
        }

        public override DynamicMetaObject FallbackInvoke(
            DynamicMetaObject target, DynamicMetaObject[] args,
            DynamicMetaObject? errorSuggestion)
        {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue || args.Any(a => !a.HasValue))
            {
                return Defer(target, args);
            }
            // Find our own binding.
            if (typeof(CrispyCallable).IsAssignableFrom(target.LimitType))
            {
                var invokeExpression = Expression.Call(
                    Expression.Convert(target.Expression, typeof(CrispyCallable)),
                    CrispyCallableInvokeMethod,
                    Expression.NewArrayInit(typeof(object), args.Select(arg => RuntimeHelpers.EnsureObjectResult(arg.Expression))));
                return new DynamicMetaObject(
                    RuntimeHelpers.EnsureObjectResult(invokeExpression),
                    target.Restrictions
                        .Merge(BindingRestrictions.Combine(args))
                        .Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)));
            }

            if (target.LimitType.IsSubclassOf(typeof(Delegate)))
            {
                var invokeMethod = target.LimitType.GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    var parms = invokeMethod.GetParameters();
                    if (parms.Length == args.Length)
                    {
                        // Don't need to check if argument types match parameters.
                        // If they don't, users get an argument conversion error.
                        var callArgs = RuntimeHelpers.ConvertArguments(args, parms);
                        var expression = Expression.Invoke(
                            Expression.Convert(target.Expression, target.LimitType),
                            callArgs);
                        return new DynamicMetaObject(
                            RuntimeHelpers.EnsureObjectResult(expression),
                            BindingRestrictions.GetTypeRestriction(target.Expression,
                                target.LimitType));
                    }
                }
            }
            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    target, args,
                    BindingRestrictions.GetTypeRestriction(target.Expression,
                        target.LimitType),
                    typeof(InvalidOperationException),
                    "Wrong number of arguments for function -- " +
                    target.LimitType.ToString() + " got " + args.ToString());

        }
    }
}
