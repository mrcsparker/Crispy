using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Crispy
{
    [SuppressMessage(
        "Naming",
        "CA1724:Type names should not match namespaces",
        Justification = "Compatibility wrapper for existing callers; prefer CrispyRuntime for new code.")]
    public class Crispy : CrispyRuntime
    {
        public Crispy(Assembly[] assms)
            : base(assms)
        {
        }

        public Crispy(Assembly[] assms, object[] instanceObjects)
            : base(assms, instanceObjects)
        {
        }
    }
}
