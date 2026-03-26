using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Crispy.Binders;

namespace Crispy.Helpers
{
    internal static class DynamicObjectHelpers
    {
        static internal object Sentinel { get; } = new object();

        internal static bool HasMember(IDynamicMetaObjectProvider o, string name)
        {
            return (GetMember(o, name) != Sentinel);
        }

        static private readonly Dictionary<string, CallSite<Func<CallSite, object, object>>> GetSites = [];

        internal static object GetMember(IDynamicMetaObjectProvider o, string name)
        {
            CallSite<Func<CallSite, object, object>>? site;
            if (!GetSites.TryGetValue(name, out site) || site == null)
            {
                site = CallSite<Func<CallSite, object, object>>.Create(new DoHelpersGetMemberBinder(name));
                GetSites[name] = site;
            }
            return site.Target(site, o);
        }

        static private readonly Dictionary<string, CallSite<Action<CallSite, object, object?>>> SetSites = [];

        internal static void SetMember(IDynamicMetaObjectProvider o, string name, object? value)
        {
            CallSite<Action<CallSite, object, object?>>? site;
            if (!SetSites.TryGetValue(name, out site) || site == null)
            {
                site = CallSite<Action<CallSite, object, object?>>.Create(new DoHelpersSetMemberBinder(name));
                SetSites[name] = site;
            }
            site.Target(site, o, value);
        }
    }
}
