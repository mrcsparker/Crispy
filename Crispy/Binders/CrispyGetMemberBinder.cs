using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Binders
{
    /// <summary>
    /// At runtime, when trying to access a member of a .NET static object,
    /// the default meta-object will call FallbackGetMember
    /// 
    /// It is used for dotted expressions for fetching members
    /// </summary>
    internal sealed class CrispyGetMemberBinder : GetMemberBinder
    {
        private static readonly MethodInfo GetExpandoMemberMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetExpandoMember)) ??
            throw new InvalidOperationException("RuntimeHelpers.GetExpandoMember was not found.");

        public CrispyGetMemberBinder(string name) : base(name, true)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
        {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue)
            {
                return Defer(target);
            }

            if (target.Value is ExpandoObject)
            {
                return new DynamicMetaObject(
                    RuntimeHelpers.EnsureObjectResult(
                        Expression.Call(
                            GetExpandoMemberMethod,
                            Expression.Convert(target.Expression, typeof(ExpandoObject)),
                            Expression.Constant(Name))),
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            }

            // Find our own binding.
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Static |
                                       BindingFlags.Instance | BindingFlags.Public;
            var members = target.LimitType.GetMember(Name, flags);
            if (members.Length == 1)
            {
                var declaringType = members[0].DeclaringType ?? target.LimitType;
                return new DynamicMetaObject(
                    RuntimeHelpers.EnsureObjectResult(
                        Expression.MakeMemberAccess(
                            Expression.Convert(
                                target.Expression,
                                declaringType),
                            members[0])),
                    // Don't need restriction test for name since this
                    // rule is only used where binder is used, which is
                    // only used in sites with this binder.Name.
                    BindingRestrictions.GetTypeRestriction(
                        target.Expression,
                        target.LimitType));
            }
            return errorSuggestion ??
                   RuntimeHelpers.CreateThrow(
                       target, null,
                       BindingRestrictions.GetTypeRestriction(target.Expression,
                                                              target.LimitType),
                       typeof(MissingMemberException),
                       "cannot bind member, " + Name +
                       ", on object " + (target.Value?.ToString() ?? "null"));
        }
    }

}
