using System.IO;
using Crispy.Parsing;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class TokenizerTest
    {
        private Tokenizer GetTokenizer(string expression)
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
    }
}
