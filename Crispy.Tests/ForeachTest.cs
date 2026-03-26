using System.Dynamic;
using System.Reflection;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class ForeachTest
    {
        private static CrispyRuntime CreateRuntime(params Assembly[] extraAssemblies)
        {
            return new CrispyRuntime([typeof(object).Assembly, .. extraAssemblies]);
        }

        private static object Execute(CrispyRuntime crispy, string text, ExpandoObject? scope = null)
        {
            return crispy.ExecuteExpr(text, scope ?? new ExpandoObject());
        }

        [Test]
        public void ShouldIterateListLiteral()
        {
            var result = Execute(CreateRuntime(), @"
                var total = 0

                foreach (value in [1, 2, 3]) {
                    total = total + value
                }

                total
            ");

            Assert.AreEqual(6, result);
        }

        [Test]
        public void ShouldSupportBreakAndContinueInsideForeach()
        {
            var result = Execute(CreateRuntime(), @"
                var total = 0

                foreach (value in [1, 2, 3, 4, 5]) {
                    if (value == 2) {
                        continue
                    }

                    if (value == 5) {
                        break
                    }

                    total = total + value
                }

                total
            ");

            Assert.AreEqual(8, result);
        }

        [Test]
        public void ShouldIterateOverStringCharacters()
        {
            var result = Execute(CreateRuntime(), @"
                var total = 0

                foreach (ch in 'crispy') {
                    total = total + 1
                }

                total
            ");

            Assert.AreEqual(6, result);
        }

        [Test]
        public void ShouldSupportNestedForeachLoops()
        {
            var result = Execute(CreateRuntime(), @"
                var total = 0

                foreach (outer in [1, 2]) {
                    foreach (inner in [1, 2, 3]) {
                        if (inner == 2) {
                            continue
                        }

                        total = total + outer + inner
                    }
                }

                total
            ");

            Assert.AreEqual(14, result);
        }

        [Test]
        public void ShouldCaptureFreshForeachVariablePerIteration()
        {
            var result = Execute(CreateRuntime(), @"
                var first = lambda() { 0 }
                var second = lambda() { 0 }
                var index = 0

                foreach (value in [10, 20]) {
                    if (index == 0) {
                        first = lambda() { value }
                    } else {
                        second = lambda() { value }
                    }

                    index = index + 1
                }

                first() + second()
            ");

            Assert.AreEqual(30, result);
        }
    }
}
