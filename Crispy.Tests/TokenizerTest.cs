using System.IO;
using Crispy.Parsing;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class TokenizerTest
    {
        private static Tokenizer GetTokenizer(string expression)
        {
            return new Tokenizer(new StringReader(expression));
        }

        [Test]
        public void ShouldCreateASingleToken()
        {
            Tokenizer tokenizer = GetTokenizer("1");
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
        }

        [Test]
        public void ShouldCreateAddition()
        {
            Tokenizer tokenizer = GetTokenizer("1 + 1");

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.Plus);

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
        }

        [Test]
        public void ShouldCreateGreaterThan()
        {
            Tokenizer tokenizer = GetTokenizer("1 > 1");

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.GreaterThan);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
        }

        [Test]
        public void ShouldCreateLessThan()
        {
            Tokenizer tokenizer = GetTokenizer("1 < 1");

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.LessThan);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
        }

        [Test]
        public void ShouldCreateMultipleParen()
        {
            Tokenizer tokenizer = GetTokenizer("(1 < 1) or (1 > 1)");

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.OpenParen);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.LessThan);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.CloseParen);

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.DoubleBar);

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.OpenParen);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.GreaterThan);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.CloseParen);
        }

        [Test]
        public void ShouldCreateParen()
        {
            Tokenizer tokenizer = GetTokenizer("(1 < 1)");

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.OpenParen);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.LessThan);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.CloseParen);
        }

        [Test]
        public void ShouldCreateSubtraction()
        {
            Tokenizer tokenizer = GetTokenizer("1 - 1");

            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.Minus);
            Assert.AreEqual(tokenizer.NextToken().Type, TokenType.NumberInteger);
        }

        [Test]
        public void ShouldCreateNullKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("null");

            Assert.AreEqual(TokenType.KeywordNull, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateDictKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("dict");

            Assert.AreEqual(TokenType.KeywordDict, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateContinueKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("continue");

            Assert.AreEqual(TokenType.KeywordContinue, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateForeachKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("foreach");

            Assert.AreEqual(TokenType.KeywordForeach, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateInKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("in");

            Assert.AreEqual(TokenType.KeywordIn, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateTryKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("try");

            Assert.AreEqual(TokenType.KeywordTry, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateCatchKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("catch");

            Assert.AreEqual(TokenType.KeywordCatch, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateFinallyKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("finally");

            Assert.AreEqual(TokenType.KeywordFinally, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateThrowKeyword()
        {
            Tokenizer tokenizer = GetTokenizer("throw");

            Assert.AreEqual(TokenType.KeywordThrow, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateBitwiseAndShiftOperators()
        {
            Tokenizer tokenizer = GetTokenizer("~1 & 2 | 3 ^^ 4 << 1 >> 2");

            Assert.AreEqual(TokenType.Tilde, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Amphersand, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Bar, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.DoubleCaret, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.LeftShift, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.RightShift, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateQuestionToken()
        {
            Tokenizer tokenizer = GetTokenizer("a ? b : c");

            Assert.AreEqual(TokenType.Identifier, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Question, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Identifier, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Colon, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Identifier, tokenizer.NextToken().Type);
        }

        [Test]
        public void ShouldCreateWordFormOperatorAliases()
        {
            Tokenizer tokenizer = GetTokenizer("not false and true or 10 mod 4 xor 1 eq 1");

            Assert.AreEqual(TokenType.Exclamation, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.KeywordFalse, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.DoubleAmphersand, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.KeywordTrue, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.DoubleBar, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.Percent, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.DoubleCaret, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.DoubleEqual, tokenizer.NextToken().Type);
            Assert.AreEqual(TokenType.NumberInteger, tokenizer.NextToken().Type);
        }
    }
}
