namespace Crispy.Ast
{
    sealed class CallableParameter
    {
        public CallableParameter(string name, NodeExpression? defaultValue = null)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public NodeExpression? DefaultValue { get; }
    }
}
