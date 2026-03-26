using System.Dynamic;
using Crispy.Parsing;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class OperatorTest
    {
        private readonly CrispyRuntime _crispy = new([typeof(object).Assembly]);
        private readonly ExpandoObject _scope = new();

        [Test]
        public void ShouldEvaluateBitwiseOperators()
        {
            Assert.AreEqual(1, _crispy.ExecuteExpr("5 & 3", _scope));
            Assert.AreEqual(7, _crispy.ExecuteExpr("5 | 2", _scope));
            Assert.AreEqual(6, _crispy.ExecuteExpr("5 ^^ 3", _scope));
        }

        [Test]
        public void ShouldEvaluateShiftOperators()
        {
            Assert.AreEqual(12, _crispy.ExecuteExpr("3 << 2", _scope));
            Assert.AreEqual(2, _crispy.ExecuteExpr("8 >> 2", _scope));
        }

        [Test]
        public void ShouldEvaluateUnaryOnesComplement()
        {
            Assert.AreEqual(-6, _crispy.ExecuteExpr("~5", _scope));
        }

        [Test]
        public void ShouldGiveBitwiseOperatorsHigherPrecedenceThanComparison()
        {
            Assert.AreEqual(true, _crispy.ExecuteExpr("5 & 3 == 1", _scope));
            Assert.AreEqual(12, _crispy.ExecuteExpr("1 + 2 << 2", _scope));
        }

        [Test]
        public void ShouldRejectTernaryOperator()
        {
            var ex = Assert.Throws<ParserException>(() => _crispy.ExecuteExpr("true ? 1 : 2", _scope));

            Assert.That(ex?.Message, Is.EqualTo("Ternary operator is not supported."));
        }

        [Test]
        public void ShouldSupportWordFormLogicalAndComparisonAliases()
        {
            Assert.AreEqual(true, _crispy.ExecuteExpr("4 eq 4", _scope));
            Assert.AreEqual(false, _crispy.ExecuteExpr("true and false", _scope));
            Assert.AreEqual(true, _crispy.ExecuteExpr("true or false", _scope));
        }

        [Test]
        public void ShouldSupportWordFormArithmeticAndUnaryAliases()
        {
            Assert.AreEqual(2, _crispy.ExecuteExpr("10 mod 4", _scope));
            Assert.AreEqual(true, _crispy.ExecuteExpr("not false", _scope));
            Assert.AreEqual(6, _crispy.ExecuteExpr("5 xor 3", _scope));
        }
    }
}
