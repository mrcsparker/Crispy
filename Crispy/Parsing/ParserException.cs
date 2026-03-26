#region

using System;

#endregion

namespace Crispy.Parsing
{
    public class ParserException : Exception
    {
        public ParserException()
        {
        }

        public ParserException(string msg)
            : base(msg)
        {
        }

        public ParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
