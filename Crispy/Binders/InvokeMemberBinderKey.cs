using System;
using System.Dynamic;

namespace Crispy.Binders
{
    internal sealed class InvokeMemberBinderKey
    {
        public string Name { get; }
        public CallInfo Info { get; }

        public InvokeMemberBinderKey(string name, CallInfo info)
        {
            Name = name;
            Info = info;
        }

        public override bool Equals(object? obj)
        {
            var key = obj as InvokeMemberBinderKey;
            // Don't lower the name.  Crispy is case-preserving in the metadata
            // in case some DynamicMetaObject ignores ignoreCase.  This makes
            // some interop cases work, but the cost is that if a Crispy program
            // spells ".foo" and ".Foo" at different sites, they won't share rules.
            return key != null && key.Name == Name && key.Info.Equals(Info);
        }

        public override int GetHashCode()
        {
            // Stolen from DLR sources when it overrode GetHashCode on binders.
            return 0x28000000 ^ Name.GetHashCode(StringComparison.Ordinal) ^ Info.GetHashCode();
        }

    }
}
