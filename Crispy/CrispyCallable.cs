using System;

namespace Crispy
{
    internal sealed class CrispyCallable
    {
        private readonly Func<object[], object> _implementation;

        public CrispyCallable(Func<object[], object> implementation, int requiredParameterCount, int totalParameterCount)
        {
            ArgumentNullException.ThrowIfNull(implementation);
            _implementation = implementation;
            RequiredParameterCount = requiredParameterCount;
            TotalParameterCount = totalParameterCount;
        }

        public int RequiredParameterCount { get; }

        public int TotalParameterCount { get; }

        public static CrispyCallable Create(Func<object[], object> implementation, int requiredParameterCount, int totalParameterCount)
        {
            return new CrispyCallable(implementation, requiredParameterCount, totalParameterCount);
        }

        public object Invoke(object[] arguments)
        {
            ArgumentNullException.ThrowIfNull(arguments);
            return arguments.Length < RequiredParameterCount || arguments.Length > TotalParameterCount
                ? throw new InvalidOperationException(
                    $"Wrong number of arguments for function -- expected {RequiredParameterCount} to {TotalParameterCount} got {arguments.Length}")
                : _implementation(arguments);
        }
    }
}
