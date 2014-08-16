using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Crispy.Parsing
{
    /// <summary>
    ///     A lexical scanner.  Takes text and turns it into tokens.
    /// </summary>
    public class Tokenizer
    {
        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
            {
                {"if", TokenType.KeywordIf},
                {"then", TokenType.KeywordThen},
                {"else", TokenType.KeywordElse},
                {"elseif", TokenType.KeywordElseIf},
                {"endif", TokenType.KeywordEnd},
                {"end", TokenType.KeywordEnd},
                {"var", TokenType.KeywordVar},
                {"or", TokenType.DoubleBar},
                {"and", TokenType.DoubleAmphersand},
                {"eq", TokenType.DoubleEqual},
                {"true", TokenType.KeywordTrue},
                {"false", TokenType.KeywordFalse},
                {"function", TokenType.KeywordFunction},
                {"defun", TokenType.KeywordFunction},
                {"lambda", TokenType.KeywordLambda},
                {"return", TokenType.KeywordReturn},
                {"break", TokenType.KeywordBreak},
                {"loop", TokenType.KeywordLoop},
                {"import", TokenType.KeywordImport},
                {"as", TokenType.KeywordAs},
                {"new", TokenType.KeywordNew}
            };

        private static readonly Dictionary<string, TokenType> Operators = new Dictionary<string, TokenType>
            {
                {"!", TokenType.Exclamation},
                {"!=", TokenType.ExclamationEqual},
                {"%", TokenType.Percent},
                {"&", TokenType.Amphersand},
                {"&&", TokenType.DoubleAmphersand},
                {"(", TokenType.OpenParen},
                {")", TokenType.CloseParen},
                {"*", TokenType.Asterisk},
                {"**", TokenType.Caret},
                {"+", TokenType.Plus},
                {"^", TokenType.Caret},
                {"^^", TokenType.DoubleCaret},
                {",", TokenType.Comma},
                {"-", TokenType.Minus},
                {".", TokenType.Dot},
                {"/", TokenType.Slash},
                {":", TokenType.Colon},
                {"~", TokenType.Tilde},
                {"<", TokenType.LessThan},
                {"<=", TokenType.LessThanOrEqual},
                {"<>", TokenType.LessThanOrGreater},
                {"<<", TokenType.LeftShift},
                {"=", TokenType.Equal},
                {"==", TokenType.DoubleEqual},
                {">", TokenType.GreaterThan},
                {">=", TokenType.GreaterThanOrEqual},
                {">>", TokenType.RightShift},
                {"?", TokenType.Question},
                {";", TokenType.SemiColon},
                {"|", TokenType.Bar},
                {"||", TokenType.DoubleBar},
                {"{", TokenType.KeywordThen},
                {"}", TokenType.KeywordEnd},
                {"[", TokenType.OpenBracket},
                {"]", TokenType.CloseBracket}
            };

        private static readonly string SingleOps = MakeSingleOps();
        private static readonly string PrefixOps = MakePrefixOps();
        private static readonly string SuffixOps = MakeSuffixOps();
        private readonly PositionalTextReader _reader;
        private char _currentChar;

        /// <summary>
        ///     Creates a positional text reader from
        ///     a TextReader
        /// </summary>
        /// <param name="text">Text to be read</param>
        public Tokenizer(TextReader text)
        {
            _reader = new PositionalTextReader(text, text.GetType().Name);
            NextChar();
        }

        private int CurrentReaderValue { get; set; }

        public bool EndOfFile
        {
            get { return CurrentReaderValue == -1; }
        }

        /// <summary>
        ///     Get the next token from the parse string.
        /// </summary>
        /// <returns></returns>
        public Token NextToken()
        {
            IgnoreWhiteSpace();

            if (IsComment())
            {
                SkipComment();
                return NextToken();
            }

            if (IsIdentifier())
            {
                return MakeIdentifier();
            }

            if (IsNumber())
            {
                return MakeNumber();
            }

            if (IsString())
            {
                return MakeString();
            }

            if (IsCombinedOperator())
            {
                return MakeCombinedOperator();
            }

            if (IsOperator())
            {
                return MakeOperator();
            }

            if (CurrentReaderValue == -1)
            {
                return MakeToken(TokenType.End, "");
            }

            throw TokenizerException(string.Format("Could not parse character: {0}", _currentChar));
        }

        private void IgnoreWhiteSpace()
        {
            while (char.IsWhiteSpace(_currentChar))
                NextChar();
        }

        private bool IsIdentifier()
        {
            return char.IsLetter(_currentChar) || _currentChar == '@' || _currentChar == '_';
        }

        private Token MakeIdentifier()
        {
            var identifierStr = new StringBuilder();

            do
            {
                identifierStr.Append(_currentChar);
                NextChar();
            } while (Char.IsLetterOrDigit(_currentChar) || _currentChar == '_');

            string ident = identifierStr.ToString();
            if (Keywords.ContainsKey(ident))
            {
                return MakeToken(Keywords[ident], ident);
            }
            return MakeToken(TokenType.Identifier, ident);
        }

        private bool IsString()
        {
            return _currentChar == '"' || _currentChar == '\'';
        }

        private Token MakeString()
        {
            var str = new StringBuilder();

            char quote = _currentChar;

            NextChar();

            while ((_currentChar != quote) && CurrentReaderValue != -1)
            {
                str.Append(_currentChar);
                NextChar();
            }

            if (_currentChar != quote)
            {
                throw TokenizerException("Quoted string was not terminated");
            }

            NextChar();

            return MakeToken(TokenType.StringLiteral, str.ToString());
        }

        private bool IsNumber()
        {
            return char.IsNumber(_currentChar);
        }

        /// <summary>
        /// A number is a number.
        ///
        /// Well...
        ///
        /// except when it is an integer.
        ///
        /// or an Int32... or Int64
        ///
        /// Int16?
        ///
        /// Oh - and double.  Or a float, which is like a small
        /// double?  Could be a decimal.  BigNum too.
        ///
        /// Don't start with long.
        ///
        /// This is C#, which sounds like C, but it isn't really C at all.  So
        /// a char is not an int that gets converted to a long.  Not sure.
        ///
        /// These are just bytes of data.
        ///
        /// Anyways, we use doubles, which are 64 bit floats.
        ///
        /// And we use Int32, which is a 32 bit integer.
        ///
        /// We used to just use Double, until I tried to do:
        ///
        ///     a[0]
        ///
        /// with a Double and you can only use Int(32|64) for an array index.
        /// (it would be kind of cool to be able to do a[3.14], but I think that I am
        /// the only one who probably thinks that)
        ///
        /// Int32's and Doubles don't convert well.  Can't just do an Expression.Convert.  .NET
        /// complains because .NET is silly sometimes.  Or maybe it is the right thing to do, 
        /// because Anders Hejlsberg is a genius.
        /// </summary>
        /// <returns>The number.</returns>
        private Token MakeNumber()
        {
            var numberStr = new StringBuilder();
            var numberType = TokenType.NumberInteger;

            do
            {
                numberStr.Append(_currentChar);
                NextChar();
            } while (Char.IsDigit(_currentChar));

            if (_currentChar == '.')
            {
                numberType = TokenType.NumberFloat;
                numberStr.Append(_currentChar);
                NextChar();

                do
                {
                    numberStr.Append(_currentChar);
                    NextChar();
                } while (Char.IsDigit(_currentChar));
            }

            if (_currentChar == 'E' || _currentChar == 'e')
            {
                numberType = TokenType.NumberFloat;
                numberStr.Append(_currentChar);
                NextChar();

                if (_currentChar == '+' || _currentChar == '-')
                {
                    numberStr.Append(_currentChar);
                    NextChar();
                }

                do
                {
                    numberStr.Append(_currentChar);
                    NextChar();
                } while (Char.IsDigit(_currentChar));
            }

            if (_currentChar == 'F' || _currentChar == 'f')
            {
                numberType = TokenType.NumberFloat;
                numberStr.Append(_currentChar);
                NextChar();
            }

            return MakeToken(numberType, numberStr.ToString());
        }

        private bool IsComment()
        {
            var nextChar = (char)_reader.Peek();
            return _currentChar == '/' && nextChar == '/';
        }

        private void SkipComment()
        {
            NextChar();
            for (; ; )
            {
                if (_currentChar == '\n' || _currentChar == '\r')
                    break;
                NextChar();
            }
            NextChar();
        }

        private bool IsCombinedOperator()
        {
            return PrefixOps.IndexOf(_currentChar) >= 0 &&
                   SuffixOps.IndexOf((char)_reader.Peek()) >= 0;
        }

        private Token MakeCombinedOperator()
        {
            var str = new StringBuilder();
            str.Append(_currentChar);
            NextChar();

            while (true)
            {
                if (SuffixOps.IndexOf(_currentChar) < 0)
                {
                    break;
                }
                str.Append(_currentChar);
                NextChar();
            }

            return MakeToken(Operators[str.ToString()], str.ToString());
        }

        private bool IsOperator()
        {
            return SingleOps.IndexOf(_currentChar) >= 0;
        }

        private Token MakeOperator()
        {
            Token t = MakeToken(Operators[_currentChar.ToString(CultureInfo.InvariantCulture)],
                                _currentChar.ToString(CultureInfo.InvariantCulture));
            NextChar();
            return t;
        }

        private void NextChar()
        {
            CurrentReaderValue = _reader.Read();
            _currentChar = (char)CurrentReaderValue;
        }

        public char CurrentChar
        {
            get { return _currentChar; }
        }

        private Token MakeToken(TokenType type, string value = null)
        {
            string tokenValue = value ?? _currentChar.ToString(CultureInfo.InvariantCulture);

            return new Token(type, tokenValue, _reader.LineNumber, _reader.ColumnNumber);
        }

        private TokenizerException TokenizerException(string msg)
        {
            return new TokenizerException(msg, _reader);
        }

        private static string MakeSingleOps()
        {
            var str = new StringBuilder();

            foreach (var op in Operators)
            {
                if (op.Key.Length == 1)
                {
                    str.Append(op.Key);
                }
            }

            return str.ToString();
        }

        private static string MakePrefixOps()
        {
            var str = new StringBuilder();

            foreach (var op in Operators)
            {
                if (op.Key.Length == 2)
                {
                    str.Append(op.Key[0]);
                }
            }

            return str.ToString();
        }

        private static string MakeSuffixOps()
        {
            var str = new StringBuilder();

            foreach (var op in Operators)
            {
                if (op.Key.Length == 2)
                {
                    str.Append(op.Key[1]);
                }
            }
            return str.ToString();
        }
    }
}
