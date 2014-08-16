using System.Dynamic;

namespace Crispy.Binders
{
    public class InvokeMemberBinderKey
    {
        readonly string _name;
        readonly CallInfo _info;

        public InvokeMemberBinderKey(string name, CallInfo info)
        {
            _name = name;
            _info = info;
        }

        public string Name { get { return _name; } }
        public CallInfo Info { get { return _info; } }

        public override bool Equals(object obj)
        {
            var key = obj as InvokeMemberBinderKey;
            // Don't lower the name.  Crispy is case-preserving in the metadata
            // in case some DynamicMetaObject ignores ignoreCase.  This makes
            // some interop cases work, but the cost is that if a Crispy program
            // spells ".foo" and ".Foo" at different sites, they won't share rules.
            return key != null && key._name == _name && key._info.Equals(_info);
        }

        public override int GetHashCode()
        {
            // Stolen from DLR sources when it overrode GetHashCode on binders.
            return 0x28000000 ^ _name.GetHashCode() ^ _info.GetHashCode();
        }

    }
}
