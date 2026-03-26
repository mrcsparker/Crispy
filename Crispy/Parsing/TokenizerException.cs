using System;
using System.Globalization;

namespace Crispy.Parsing
{
    public sealed class TokenizerException : Exception
    {
        private readonly int _columnNumber;
        private readonly string _fileName;
        private readonly int _lineNumber;
        private readonly string _msg;

        public TokenizerException()
            : this(string.Empty)
        {
        }

        public TokenizerException(string msg)
            : this(msg, "", -1, -1)
        {
        }

        public TokenizerException(string msg, Exception innerException)
            : base(msg, innerException)
        {
            _msg = msg;
            _fileName = string.Empty;
            _lineNumber = -1;
            _columnNumber = -1;
        }

        public TokenizerException(string msg, PositionalTextReader reader)
            : this(
                msg,
                (reader ?? throw new ArgumentNullException(nameof(reader))).Source,
                reader.LineNumber,
                reader.ColumnNumber)
        {
        }

        public TokenizerException(string msg, int lineNumber, int columnNumber)
            : this(msg, "", lineNumber, columnNumber)
        {
        }

        public TokenizerException(string msg, string fileName, int lineNumber, int columnNumber)
            : base(msg)
        {
            _msg = msg;
            _fileName = fileName;
            _lineNumber = lineNumber;
            _columnNumber = columnNumber;
        }

        public override string Message
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1}:{2}): {3}",
                    _fileName,
                    _lineNumber,
                    _columnNumber,
                    _msg);
            }
        }
    }
}
