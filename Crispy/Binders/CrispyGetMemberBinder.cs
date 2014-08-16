using System;
using System.Dynamic;
using System.Reflection;
using Crispy.Helpers;
using System.Linq.Expressions;

namespace Crispy.Binders
{
    /// <summary>
    /// At runtime, when trying to access a member of a .NET static object,
    /// the default meta-object will call FallbackGetMember
    /// 
    /// It is used for dotted expressions for fetching members
    /// </summary>
    public class CrispyGetMemberBinder : GetMemberBinder {
        public CrispyGetMemberBinder(string name) : base(name, true) {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject targetMO, DynamicMetaObject errorSuggestion) {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue) return Defer(targetMO);
            // Find our own binding.
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Static |
                                       BindingFlags.Instance | BindingFlags.Public;
            var members = targetMO.LimitType.GetMember(Name, flags);
            if (members.Length == 1) {
                return new DynamicMetaObject(
                    RuntimeHelpers.EnsureObjectResult(
                        Expression.MakeMemberAccess(
                            Expression.Convert(targetMO.Expression,
                                members[0].DeclaringType),
                            members[0])),
                    // Don't need restriction test for name since this
                    // rule is only used where binder is used, which is
                    // only used in sites with this binder.Name.
                    BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                        targetMO.LimitType));
            }
            return errorSuggestion ??
                   RuntimeHelpers.CreateThrow(
                       targetMO, null,
                       BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                              targetMO.LimitType),
                       typeof(MissingMemberException),
                       "cannot bind member, " + Name +
                       ", on object " + targetMO.Value.ToString());
        }
    }

}

