namespace Crispy.Repl
{
    public sealed class ReplSubmissionResult
    {
        private ReplSubmissionResult(ReplSubmissionKind kind, object? value, string? displayText)
        {
            Kind = kind;
            Value = value;
            DisplayText = displayText;
        }

        public ReplSubmissionKind Kind { get; }

        public object? Value { get; }

        public string? DisplayText { get; }

        public static ReplSubmissionResult None()
        {
            return new ReplSubmissionResult(ReplSubmissionKind.None, null, null);
        }

        public static ReplSubmissionResult Incomplete()
        {
            return new ReplSubmissionResult(ReplSubmissionKind.Incomplete, null, null);
        }

        public static ReplSubmissionResult Executed(object? value, string displayText)
        {
            return new ReplSubmissionResult(ReplSubmissionKind.Executed, value, displayText);
        }

        public static ReplSubmissionResult Info(string displayText)
        {
            return new ReplSubmissionResult(ReplSubmissionKind.Info, null, displayText);
        }

        public static ReplSubmissionResult Error(string displayText)
        {
            return new ReplSubmissionResult(ReplSubmissionKind.Error, null, displayText);
        }

        public static ReplSubmissionResult Exit()
        {
            return new ReplSubmissionResult(ReplSubmissionKind.Exit, null, null);
        }
    }
}
