using System;
using System.Dynamic;
using System.Reflection;
using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Binders
{
    /// <summary>
    /// Used for setting got expression members
    /// </summary>
    internal sealed class CrispySetMemberBinder : SetMemberBinder
    {
        public CrispySetMemberBinder(string name)
            : base(name, true)
        {
        }

        public override DynamicMetaObject FallbackSetMember(
            DynamicMetaObject target, DynamicMetaObject value,
            DynamicMetaObject? errorSuggestion)
        {

            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue) return Defer(target);
            // Find our own binding.
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Static |
                BindingFlags.Instance | BindingFlags.Public;
            var members = target.LimitType.GetMember(Name, flags);
            if (members.Length == 1)
            {
                MemberInfo mem = members[0];
                var declaringType = mem.DeclaringType ?? target.LimitType;
                Expression val;
                // Should check for member domain type being Type and value being
                // TypeModel, similar to ConvertArguments, and building an
                // expression like GetRuntimeTypeMoFromModel.
                if (mem.MemberType == MemberTypes.Property)
                    val = Expression.Convert(value.Expression,
                        ((PropertyInfo)mem).PropertyType);
                else if (mem.MemberType == MemberTypes.Field)
                    val = Expression.Convert(value.Expression,
                        ((FieldInfo)mem).FieldType);
                else
                    return (errorSuggestion ??
                        RuntimeHelpers.CreateThrow(
                            target, null,
                            BindingRestrictions.GetTypeRestriction(
                                target.Expression,
                                target.LimitType),
                            typeof(InvalidOperationException),
                            "Crispy only supports setting Properties and " +
                            "fields at this time."));
                return new DynamicMetaObject(
                    // Assign returns the stored value, so we're good for Crispy.
                    RuntimeHelpers.EnsureObjectResult(
                            Expression.Assign(
                                Expression.MakeMemberAccess(
                                    Expression.Convert(target.Expression,
                                        declaringType),
                                members[0]),
                            val)),
                    // Don't need restriction test for name since this
                    // rule is only used where binder is used, which is
                    // only used in sites with this binder.Name.
                    BindingRestrictions.GetTypeRestriction(target.Expression,
                        target.LimitType));
            }
            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    target, null,
                    BindingRestrictions.GetTypeRestriction(target.Expression,
                        target.LimitType),
                    typeof(MissingMemberException),
                    "IDynObj member name conflict.");
        }
    }
}
