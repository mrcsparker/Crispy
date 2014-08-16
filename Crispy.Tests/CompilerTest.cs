using System.Dynamic;
using System.Reflection;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class CompilerTest
    {
        private readonly Crispy _Crispy;
        private readonly ExpandoObject _scope = new ExpandoObject();

        public CompilerTest()
        {
            string dllPath = typeof(object).Assembly.Location;
            Assembly asm = Assembly.LoadFile(dllPath);
            _Crispy = new Crispy(new[] { asm });
        }

        [Test]
        public void TestAdditiveNode()
        {
            var result0 = (int) _Crispy.ExecuteExpr("100 + 2", _scope);
            Assert.AreEqual(102, result0);

            var result1 = (double)_Crispy.ExecuteExpr("100.1 + 100.2", _scope);
            Assert.AreEqual(200.3, result1);

            var result2 = (int)_Crispy.ExecuteExpr("1-1", _scope);
            Assert.AreEqual(0, result2);
        }

        [Test]
        public void TestComparisons()
        {
            bool boolResult = (bool)_Crispy.ExecuteExpr("3 > 2", _scope);
            Assert.IsTrue(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("3 < 2", _scope);
            Assert.IsFalse(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("5 == 5", _scope);
            Assert.IsTrue(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("5 == 4", _scope);
            Assert.IsFalse(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("5 > 4", _scope);
            Assert.IsTrue(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("3 > 4", _scope);
            Assert.IsFalse(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("5 < 4", _scope);
            Assert.IsFalse(boolResult);

            boolResult = (bool)_Crispy.ExecuteExpr("3 < 4", _scope);
            Assert.IsTrue(boolResult);
        }

        [Test]
        public void TestMultiplicativeNode()
        {
            var result = (int)_Crispy.ExecuteExpr("5 * 5", _scope);
            Assert.AreEqual(25, result);

            result = (int)_Crispy.ExecuteExpr("5 / 5", _scope);
            Assert.AreEqual(1, result);

            var result2 = (double)_Crispy.ExecuteExpr("2.0 ^ 2.0", _scope);
            Assert.AreEqual(4, result2);

            result = (int)_Crispy.ExecuteExpr("5 % 2", _scope);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void TestParenExpressionNode()
        {
            int result = (int)_Crispy.ExecuteExpr("(1 + 1) + 3 + 1", _scope);
            Assert.AreEqual(6, result);

            result = (int)_Crispy.ExecuteExpr("(1 + 1) + (3 * 5)", _scope);
            Assert.AreEqual(17, result);

            result = (int)_Crispy.ExecuteExpr("1 + 13 * (18 * 16 / 4)", _scope);
            Assert.AreEqual(937, (int) result);

            result = (int)_Crispy.ExecuteExpr("1 + 2 * (4 * 16 / 8) * 2 + 1", _scope);
            Assert.AreEqual(34, (int)result);
        }

        [Test]
        public void TestPrecedence()
        {
            var result = (int)_Crispy.ExecuteExpr("1 + 2 * 3", _scope);
            Assert.AreEqual(7, result);
        }

        [Test]
        public void TestSimpleAddition()
        {
            int result = (int)_Crispy.ExecuteExpr("1+1", _scope);
            Assert.AreEqual(2, result);
            result = (int)_Crispy.ExecuteExpr("1+1", _scope);
            Assert.AreEqual(2, result);
            result = (int)_Crispy.ExecuteExpr("1 + 1", _scope);
            Assert.AreEqual(2, result);
        }

        [Test]
        public void TestSimpleNumber()
        {
            int result = (int)_Crispy.ExecuteExpr("1", _scope);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void TestStringComparisons()
        {
            bool result = (bool)_Crispy.ExecuteExpr("'A' == 'A'", _scope);
            Assert.IsTrue(result);
        }
    }
}
