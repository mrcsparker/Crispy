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
        private static readonly Dictionary<string, TokenType> Keywords = new()
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
                {"not", TokenType.Exclamation},
                {"mod", TokenType.Percent},
                {"xor", TokenType.DoubleCaret},
                {"true", TokenType.KeywordTrue},
                {"false", TokenType.KeywordFalse},
                {"null", TokenType.KeywordNull},
                {"dict", TokenType.KeywordDict},
                {"try", TokenType.KeywordTry},
                {"catch", TokenType.KeywordCatch},
                {"finally", TokenType.KeywordFinally},
                {"throw", TokenType.KeywordThrow},
                {"function", TokenType.KeywordFunction},
                {"defun", TokenType.KeywordFunction},
                {"lambda", TokenType.KeywordLambda},
                {"return", TokenType.KeywordReturn},
                {"break", TokenType.KeywordBreak},
                {"continue", TokenType.KeywordContinue},
                {"foreach", TokenType.KeywordForeach},
                {"in", TokenType.KeywordIn},
                {"loop", TokenType.KeywordLoop},
                {"import", TokenType.KeywordImport},
                {"as", TokenType.KeywordAs},
                {"new", TokenType.KeywordNew}
            };

        private static readonly Dictionary<string, TokenType> Operators = new()
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

        private static readonly HashSet<char> SingleOps = MakeSingleOps();
        private static readonly HashSet<char> PrefixOps = MakePrefixOps();
        private static readonly HashSet<char> SuffixOps = MakeSuffixOps();
        private readonly PositionalTextReader _reader;

        /// <summary>
        ///     Creates a positional text reader from
        ///     a TextReader
        /// </summary>
        /// <param name="text">Text to be read</param>
        public Tokenizer(TextReader text)
        {
            ArgumentNullException.ThrowIfNull(text);
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
            return IsIdentifier()
                ? MakeIdentifier()
                : IsNumber()
                    ? MakeNumber()
                    : IsString()
                        ? MakeString()
                        : IsCombinedOperator()
                            ? MakeCombinedOperator()
                            : IsOperator()
                                ? MakeOperator()
                                : CurrentReaderValue == -1
                                    ? MakeToken(TokenType.End, "")
                                    : throw TokenizerException(
                                        string.Format(
                                            CultureInfo.InvariantCulture,
                                            "Could not parse character: {0}",
                                            CurrentChar));
        }

        private void IgnoreWhiteSpace()
        {
            while (char.IsWhiteSpace(CurrentChar))
            {
                NextChar();
            }
        }

        private bool IsIdentifier()
        {
            return char.IsLetter(CurrentChar) || CurrentChar == '@' || CurrentChar == '_';
        }

        private Token MakeIdentifier()
        {
            var identifierStr = new StringBuilder();

            do
            {
                identifierStr.Append(CurrentChar);
                NextChar();
            } while (Char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_');

            string ident = identifierStr.ToString();
            return Keywords.TryGetValue(ident, out var tokenType)
                ? MakeToken(tokenType, ident)
                : MakeToken(TokenType.Identifier, ident);
        }

        private bool IsString()
        {
            return CurrentChar == '"' || CurrentChar == '\'';
        }

        private Token MakeString()
        {
            var str = new StringBuilder();

            char quote = CurrentChar;

            NextChar();

            while ((CurrentChar != quote) && CurrentReaderValue != -1)
            {
                str.Append(CurrentChar);
                NextChar();
            }

            if (CurrentChar != quote)
            {
                throw TokenizerException("Quoted string was not terminated");
            }

            NextChar();

            return MakeToken(TokenType.StringLiteral, str.ToString());
        }

        private bool IsNumber()
        {
            return char.IsNumber(CurrentChar);
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
                numberStr.Append(CurrentChar);
                NextChar();
            } while (Char.IsDigit(CurrentChar));

            if (CurrentChar == '.')
            {
                numberType = TokenType.NumberFloat;
                numberStr.Append(CurrentChar);
                NextChar();

                do
                {
                    numberStr.Append(CurrentChar);
                    NextChar();
                } while (Char.IsDigit(CurrentChar));
            }

            if (CurrentChar == 'E' || CurrentChar == 'e')
            {
                numberType = TokenType.NumberFloat;
                numberStr.Append(CurrentChar);
                NextChar();

                if (CurrentChar == '+' || CurrentChar == '-')
                {
                    numberStr.Append(CurrentChar);
                    NextChar();
                }

                do
                {
                    numberStr.Append(CurrentChar);
                    NextChar();
                } while (Char.IsDigit(CurrentChar));
            }

            if (CurrentChar == 'F' || CurrentChar == 'f')
            {
                numberType = TokenType.NumberFloat;
                numberStr.Append(CurrentChar);
                NextChar();
            }

            return MakeToken(numberType, numberStr.ToString());
        }

        private bool IsComment()
        {
            var nextChar = (char)_reader.Peek();
            return CurrentChar == '/' && nextChar == '/';
        }

        private void SkipComment()
        {
            NextChar();
            for (; ; )
            {
                if (CurrentChar == '\n' || CurrentChar == '\r')
                {
                    break;
                }

                NextChar();
            }
            NextChar();
        }

        private bool IsCombinedOperator()
        {
            return PrefixOps.Contains(CurrentChar) &&
                   SuffixOps.Contains((char)_reader.Peek());
        }

        private Token MakeCombinedOperator()
        {
            var str = new StringBuilder();
            str.Append(CurrentChar);
            NextChar();

            while (true)
            {
                if (!SuffixOps.Contains(CurrentChar))
                {
                    break;
                }
                str.Append(CurrentChar);
                NextChar();
            }

            return MakeToken(Operators[str.ToString()], str.ToString());
        }

        private bool IsOperator()
        {
            return SingleOps.Contains(CurrentChar);
        }

        private Token MakeOperator()
        {
            Token t = MakeToken(Operators[CurrentChar.ToString(CultureInfo.InvariantCulture)],
                                CurrentChar.ToString(CultureInfo.InvariantCulture));
            NextChar();
            return t;
        }

        private void NextChar()
        {
            CurrentReaderValue = _reader.Read();
            CurrentChar = (char)CurrentReaderValue;
        }

        public char CurrentChar { get; private set; }

        private Token MakeToken(TokenType type, string? value = null)
        {
            string tokenValue = value ?? CurrentChar.ToString(CultureInfo.InvariantCulture);

            return new Token(type, tokenValue, _reader.LineNumber, _reader.ColumnNumber);
        }

        private TokenizerException TokenizerException(string msg)
        {
            return new TokenizerException(msg, _reader);
        }

        private static HashSet<char> MakeSingleOps()
        {
            var chars = new HashSet<char>();

            foreach (var op in Operators)
            {
                if (op.Key.Length == 1)
                {
                    chars.Add(op.Key[0]);
                }
            }

            return chars;
        }

        private static HashSet<char> MakePrefixOps()
        {
            var chars = new HashSet<char>();

            foreach (var op in Operators)
            {
                if (op.Key.Length == 2)
                {
                    chars.Add(op.Key[0]);
                }
            }

            return chars;
        }

        private static HashSet<char> MakeSuffixOps()
        {
            var chars = new HashSet<char>();

            foreach (var op in Operators)
            {
                if (op.Key.Length == 2)
                {
                    chars.Add(op.Key[1]);
                }
            }
            return chars;
        }
    }
}
