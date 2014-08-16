using System;
using System.Dynamic;
using Crispy.Helpers;

namespace Crispy.Binders
{
    class DoHelpersSetMemberBinder : SetMemberBinder {
        internal DoHelpersSetMemberBinder(string name) : base(name, true) { }

        public override DynamicMetaObject FallbackSetMember(
            DynamicMetaObject target, DynamicMetaObject value,
            DynamicMetaObject errorSuggestion) {
            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    target, null, BindingRestrictions.Empty,
                    typeof(MissingMemberException),
                    "If IDynObj doesn't support setting members, " +
                    "DOHelpers can't do it for the IDO.");
        }
    }
}

