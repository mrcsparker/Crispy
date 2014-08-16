using System;
using System.Dynamic;
using System.Reflection;
using Crispy.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Crispy.Binders
{
    /// <summary>
    /// Used for invoking function calls on dotted expressions
    /// </summary>
    public class CrispyInvokeMemberBinder : InvokeMemberBinder {
        public CrispyInvokeMemberBinder(string name, CallInfo callinfo) 
            : base(name, true, callinfo) { // true = ignoreCase
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject targetMO, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue || args.Any(a => !a.HasValue)) {
                var deferArgs = new DynamicMetaObject[args.Length + 1];
                for (int i = 0; i < args.Length; i++) {
                    deferArgs[i + 1] = args[i];
                }
                deferArgs[0] = targetMO;
                return Defer(deferArgs);
            }
            // Find our own binding.
            // Could consider allowing invoking static members from an instance.
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Instance |
                                       BindingFlags.Public;
            var members = targetMO.LimitType.GetMember(Name, flags);
            if ((members.Length == 1) && (members[0] is PropertyInfo ||
                members[0] is FieldInfo)) {
                // NEED TO TEST, should check for delegate value too
                    throw new NotImplementedException();
            }
            // Get MethodInfos with right arg counts.
            var miMems = members.
                Select(m => m as MethodInfo).
                Where(m => m.GetParameters().Length == args.Length);
            // Get MethodInfos with param types that work for args.  This works
            // except for value args that need to pass to reftype params. 
            // We could detect that to be smarter and then explicitly StrongBox
            // the args.
            var res = new List<MethodInfo>();
            foreach (var mem in miMems) {
                if (RuntimeHelpers.ParametersMatchArguments(
                    mem.GetParameters(), args)) {
                        res.Add(mem);
                    }
            }
            // False below means generate a type restriction on the MO.
            // We are looking at the members targetMO's Type.
            var restrictions = RuntimeHelpers.GetTargetArgsRestrictions(
                targetMO, args, false);
            if (res.Count == 0) {
                return errorSuggestion ??
                       RuntimeHelpers.CreateThrow(
                           targetMO, args, restrictions,
                           typeof(MissingMemberException),
                           "Can't bind member invoke -- " + args.ToString());
            }
            // restrictions and conversion must be done consistently.
            var callArgs = RuntimeHelpers.ConvertArguments(
                args, res[0].GetParameters());
            return new DynamicMetaObject(
                RuntimeHelpers.EnsureObjectResult(
                    Expression.Call(
                        Expression.Convert(targetMO.Expression,
                                           targetMO.LimitType),
                        res[0], callArgs)),
                restrictions);
            // Could hve tried just letting Expr.Call factory do the work,
            // but if there is more than one applicable method using just
            // assignablefrom, Expr.Call throws.  It does not pick a "most
            // applicable" method or any method.
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject targetMO, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) 
        {
            var argexprs = new Expression[args.Length + 1];
            for (int i = 0; i < args.Length; i++) {
                argexprs[i + 1] = args[i].Expression;
            }
            argexprs[0] = targetMO.Expression;
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
                targetMO.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }
    }
}

