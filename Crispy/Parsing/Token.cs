namespace Crispy.Parsing
{
    public class Token
    {
        public Token(TokenType type, string value, int lineNumber, int columnNumber)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public TokenType Type { get; private set; }
        public string Value { get; set; }
        public int LineNumber { get; private set; }
        public int ColumnNumber { get; private set; }

        public override string ToString()
        {
            return string.Format("({0},{1}): [{2}] {3}", LineNumber, ColumnNumber, Type, Value);
        }
    }
}
