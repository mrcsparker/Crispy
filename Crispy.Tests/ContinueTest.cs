using System;
using System.Dynamic;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class ContinueTest
    {
        private static CrispyRuntime CreateRuntime()
        {
            return new CrispyRuntime([typeof(object).Assembly]);
        }

        private static object Execute(CrispyRuntime crispy, string text, ExpandoObject? scope = null)
        {
            return crispy.ExecuteExpr(text, scope ?? new ExpandoObject());
        }

        [Test]
        public void ShouldContinueWithinLoop()
        {
            var result = Execute(CreateRuntime(), @"
                var total = 0
                var i = 0

                loop {
                    i = i + 1

                    if (i == 2) {
                        continue
                    }

                    total = total + i

                    if (i == 3) {
                        break
                    }
                }

                total
            ");

            Assert.AreEqual(4, result);
        }

        [Test]
        public void ShouldContinueInsideNestedIfWithinLoop()
        {
            var result = Execute(CreateRuntime(), @"
                var total = 0
                var i = 0

                loop {
                    i = i + 1

                    if (i < 4) {
                        if (i == 2) {
                            continue
                        }

                        total = total + i
                    } else {
                        break
                    }
                }

                total
            ");

            Assert.AreEqual(4, result);
        }

        [Test]
        public void ShouldContinueInnermostLoopOnly()
        {
            var result = Execute(CreateRuntime(), @"
                var outer = 0
                var total = 0

                loop {
                    outer = outer + 1
                    var inner = 0

                    loop {
                        inner = inner + 1

                        if (inner == 2) {
                            continue
                        }

                        total = total + inner

                        if (inner == 3) {
                            break
                        }
                    }

                    if (outer == 2) {
                        break
                    }
                }

                total
            ");

            Assert.AreEqual(8, result);
        }

        [Test]
        public void ShouldRejectContinueOutsideLoop()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Execute(CreateRuntime(), "continue"));

            Assert.That(ex?.Message, Is.EqualTo("Call to Continue not inside loop."));
        }
    }
}
