using System;
using System.Diagnostics.CodeAnalysis;

namespace Crispy.Tests.Data
{
    [SuppressMessage(
        "Design",
        "CA1051:Do not declare visible instance fields",
        Justification = "Used to verify delegate-valued field invocation from Crispy.")]
    public sealed class DelegateHost
    {
        public Func<int, int, int> AddProperty { get; } = static (a, b) => a + b;

        public object ObjectAddProperty { get; } = new Func<int, int, int>(static (a, b) => a + b);

        public readonly Func<int, int, int> MultiplyField = static (a, b) => a * b;

        public string LabelProperty { get; } = "not callable";

        public static Func<int, int, int> StaticAddProperty { get; } = static (a, b) => a + b;

        public static object StaticObjectSubtractProperty { get; } =
            new Func<int, int, int>(static (a, b) => a - b);

        [SuppressMessage(
            "Usage",
            "CA2211:Non-constant fields should not be visible",
            Justification = "Used to verify static delegate-valued field invocation from Crispy.")]
        public static readonly Func<int, int, int> StaticMultiplyField = static (a, b) => a * b;

        public static string StaticLabelProperty { get; } = "still not callable";
    }
}
