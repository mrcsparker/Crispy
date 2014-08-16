using System.IO;

namespace Crispy.Parsing
{
    public class PositionalTextReader
    {
        private readonly TextReader _reader;

        public PositionalTextReader(TextReader reader, string source)
        {
            _reader = reader;
            Source = source;
            LineNumber = 1;
            ColumnNumber = -1;
        }

        public PositionalTextReader(string s) :
            this(new StringReader(s), "<string>")
        {
        }

        public string Source { get; private set; }
        public int LineNumber { get; private set; }
        public int ColumnNumber { get; private set; }

        public int Peek()
        {
            return _reader.Peek();
        }

        public int Read()
        {
            int c = _reader.Read();

            if (c == '\n')
            {
                LineNumber++;
                ColumnNumber = 1;
            }
            else
            {
                ColumnNumber++;
            }

            return c;
        }

        public int Length()
        {
            return _reader.ToString().Length;
        }
    }
}
