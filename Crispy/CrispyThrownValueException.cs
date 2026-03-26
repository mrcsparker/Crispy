using System;
using System.Globalization;

namespace Crispy
{
    public sealed class CrispyThrownValueException : Exception
    {
        public CrispyThrownValueException()
            : this((object?)null)
        {
        }

        public CrispyThrownValueException(string message)
            : base(message)
        {
        }

        public CrispyThrownValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CrispyThrownValueException(object? value)
            : base(string.Format(
                CultureInfo.InvariantCulture,
                "Crispy threw value: {0}",
                value ?? "null"))
        {
            Value = value;
        }

        public object? Value { get; }
    }
}
