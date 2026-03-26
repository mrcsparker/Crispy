using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Binders
{
    /// <summary>
    /// Used for invoking function calls on dotted expressions
    /// </summary>
    internal sealed class CrispyInvokeMemberBinder : InvokeMemberBinder
    {
        public CrispyInvokeMemberBinder(string name, CallInfo callinfo)
            : base(name, true, callinfo)
        { // true = ignoreCase
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject? errorSuggestion)
        {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue || args.Any(a => !a.HasValue))
            {
                var deferArgs = new DynamicMetaObject[args.Length + 1];
                for (int i = 0; i < args.Length; i++)
                {
                    deferArgs[i + 1] = args[i];
                }
                deferArgs[0] = target;
                return Defer(deferArgs);
            }
            // Find our own binding.
            // Could consider allowing invoking static members from an instance.
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Instance |
                                       BindingFlags.Public;
            var members = target.LimitType.GetMember(Name, flags);
            if ((members.Length == 1) && (members[0] is PropertyInfo ||
                members[0] is FieldInfo))
            {
                var memberRestrictions = RuntimeHelpers.GetTargetArgsRestrictions(
                    target, args, false);
                var memberAccess = Expression.MakeMemberAccess(
                    Expression.Convert(target.Expression, target.LimitType),
                    members[0]);
                return new DynamicMetaObject(
                    RuntimeHelpers.MakeInvokeExpression(memberAccess, args),
                    memberRestrictions);
            }
            // False below means generate a type restriction on the MO.
            // We are looking at the members targetMO's Type.
            var restrictions = RuntimeHelpers.GetTargetArgsRestrictions(
                target, args, false);
            var resolution = RuntimeHelpers.ResolveMethodOverload(
                members.OfType<MethodInfo>(),
                args,
                "member '" + Name + "'");
            if (resolution.ErrorMessage != null)
            {
                return errorSuggestion ??
                       RuntimeHelpers.CreateThrow(
                           target, args, restrictions,
                           typeof(InvalidOperationException),
                           resolution.ErrorMessage);
            }

            if (resolution.Method == null)
            {
                return errorSuggestion ??
                       RuntimeHelpers.CreateThrow(
                           target, args, restrictions,
                           typeof(MissingMemberException),
                           "Can't bind member invoke -- " + args.ToString());
            }
            // restrictions and conversion must be done consistently.
            var method = resolution.Method;
            var callArgs = RuntimeHelpers.ConvertArguments(
                args, method.GetParameters());
            return new DynamicMetaObject(
                RuntimeHelpers.EnsureObjectResult(
                    Expression.Call(
                        Expression.Convert(target.Expression,
                                           target.LimitType),
                        method, callArgs)),
                restrictions);
            // Could hve tried just letting Expr.Call factory do the work,
            // but if there is more than one applicable method using just
            // assignablefrom, Expr.Call throws.  It does not pick a "most
            // applicable" method or any method.
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject? errorSuggestion)
        {
            var argexprs = new Expression[args.Length + 1];
            for (int i = 0; i < args.Length; i++)
            {
                argexprs[i + 1] = args[i].Expression;
            }
            argexprs[0] = target.Expression;
            // Just "defer" since we have code in CrispyInvokeBinder that knows
            // what to do, and typically this fallback is from a language like
            // Python that passes a DynamicMetaObject with HasValue == false.
            return new DynamicMetaObject(
                Expression.Dynamic(
                    // This call site doesn't share any L2 caching
                    // since we don't call GetInvokeBinder from Crispy.
                    // We aren't plumbed to get the runtime instance here.
                    new CrispyInvokeBinder(new CallInfo(args.Length)),
                    typeof(object), // ret type
                    argexprs),
                // No new restrictions since CrispyInvokeBinder will handle it.
                target.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }
    }
}
