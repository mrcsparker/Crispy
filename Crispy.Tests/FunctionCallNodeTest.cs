using System;
using System.Dynamic;
using System.Globalization;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class FunctionCallNodeTest
    {
        [Test]
        public void ShouldCallAbs()
        {
            double result = InvokeAsDouble("ABS(1)");
            Assert.AreEqual(1, result);

            result = InvokeAsDouble("ABS(2 - 3)");
            Assert.AreEqual(1, result);

            result = InvokeAsDouble("ABS(-1)");
            Assert.AreEqual(1, result);
        }

        [Test]
        public void ShouldCallRound()
        {
            double result = InvokeAsDouble("ROUND(1.01)");
            Assert.AreEqual(1, result);

            result = InvokeAsDouble("ROUND(0.1)");
            Assert.AreEqual(0, result);
        }

        private static double InvokeAsDouble(string text)
        {
            var crispy = new CrispyRuntime(new[] { typeof(object).Assembly }, new object[] { new MathFunctions() });
            return Convert.ToDouble(crispy.ExecuteExpr(text, new ExpandoObject()), CultureInfo.InvariantCulture);
        }

        private sealed class MathFunctions
        {
            public static double Abs(double value)
            {
                return Math.Abs(value);
            }

            public static double Abs(object value)
            {
                return Math.Abs(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }

            public static double Round(double value)
            {
                return Math.Round(value);
            }

            public static double Round(object value)
            {
                return Math.Round(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }
        }
    }
}
