#region

using System;

#endregion

namespace Crispy.Parsing
{
    public class ParserException : Exception
    {
        private readonly string _msg;

        public ParserException(string msg)
        {
            _msg = msg;
        }

        public override string Message
        {
            get { return String.Format("{0}", _msg); }
        }
    }
}
