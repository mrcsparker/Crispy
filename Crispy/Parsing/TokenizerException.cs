using System;

namespace Crispy.Parsing
{
    internal class TokenizerException : Exception
    {
        private readonly int _columnNumber;
        private readonly string _fileName;
        private readonly int _lineNumber;
        private readonly string _msg;

        public TokenizerException(string msg)
            : this(msg, "", -1, -1)
        {
        }

        public TokenizerException(string msg, PositionalTextReader reader)
            : this(msg, reader.Source, reader.LineNumber, reader.ColumnNumber)
        {
        }

        public TokenizerException(string msg, int lineNumber, int columnNumber)
            : this(msg, "", lineNumber, columnNumber)
        {
        }

        public TokenizerException(string msg, string fileName, int lineNumber, int columnNumber)
        {
            _msg = msg;
            _fileName = fileName;
            _lineNumber = lineNumber;
            _columnNumber = columnNumber;
        }

        public override string Message
        {
            get { return String.Format("{0} ({1}:{2}): {3}", _fileName, _lineNumber, _columnNumber, _msg); }
        }
    }
}
