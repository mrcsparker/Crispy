using System;
using System.Diagnostics.CodeAnalysis;

namespace Crispy.Tests.Data
{
    [SuppressMessage(
        "Design",
        "CA1021:Avoid out parameters",
        Justification = "Used to verify clear Crispy errors for unsupported ref/out interop.")]
    public sealed class OverloadHost
    {
        public OverloadHost(int value)
        {
            Kind = "int";
            NumericValue = value;
        }

        public OverloadHost(long value)
        {
            Kind = "long";
            NumericValue = value;
        }

        public OverloadHost(double value)
        {
            Kind = "double";
            NumericValue = value;
        }

        public string Kind { get; }

        public double NumericValue { get; }

        public string Select(int value)
        {
            return value >= NumericValue ? "int" : "int";
        }

        public string Select(double value)
        {
            return value >= NumericValue ? "double" : "double";
        }

        public string Widen(int value)
        {
            return value >= NumericValue ? "int" : "int";
        }

        public string Widen(long value)
        {
            return value >= NumericValue ? "long" : "long";
        }

        public static string StaticSelect(int value)
        {
            return value >= 0 ? "int" : "int";
        }

        public static string StaticSelect(double value)
        {
            return value >= 0 ? "double" : "double";
        }

        public string OptionalInstance(int first, int second = 1)
        {
            return (first + second + Kind.Length - Kind.Length).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string StaticOptional(int first, int second = 1)
        {
            return (first + second).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public string Ambiguous(IFormattable value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value.ToString(null, System.Globalization.CultureInfo.InvariantCulture) != null &&
                Kind.Length >= 0
                ? "iformattable"
                : string.Empty;
        }

        public string Ambiguous(IConvertible value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value.ToString(System.Globalization.CultureInfo.InvariantCulture) != null &&
                Kind.Length >= 0
                ? "iconvertible"
                : string.Empty;
        }

        public string Generic<T>(T value)
        {
            return value == null && Kind.Length < 0 ? string.Empty : typeof(T).Name;
        }

        public static string StaticGeneric<T>(T value)
        {
            return value == null ? typeof(T).Name : typeof(T).Name;
        }

        public string RefOnly(ref int value)
        {
            return value >= NumericValue
                ? value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public sealed class OptionalConstructorHost
    {
        public OptionalConstructorHost(int first, int second = 1)
        {
            Total = first + second;
        }

        public int Total { get; }
    }
}
