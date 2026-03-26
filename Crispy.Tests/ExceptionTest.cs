using System;
using System.Dynamic;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class ExceptionTest
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
        public void ShouldCatchDotNetException()
        {
            var result = Execute(CreateRuntime(), @"
                try {
                    throw new system.invalidOperationException('boom')
                } catch (err) {
                    err.Message
                }
            ");

            Assert.AreEqual("boom", result);
        }

        [Test]
        public void ShouldCatchThrownValueAsOriginalValue()
        {
            var result = Execute(CreateRuntime(), @"
                try {
                    throw 'boom'
                } catch (err) {
                    err
                }
            ");

            Assert.AreEqual("boom", result);
        }

        [Test]
        public void ShouldRunFinallyAndPreserveTryValue()
        {
            var result = Execute(CreateRuntime(), @"
                defun run() {
                    var state = 0
                    var value = try {
                        10
                    } finally {
                        state = 1
                    }

                    state + value
                }

                run()
            ");

            Assert.AreEqual(11, result);
        }

        [Test]
        public void ShouldRethrowCurrentException()
        {
            var result = Execute(CreateRuntime(), @"
                try {
                    try {
                        throw new system.argumentException('bad')
                    } catch (err) {
                        throw
                    }
                } catch (outer) {
                    outer.Message
                }
            ");

            Assert.AreEqual("bad", result);
        }

        [Test]
        public void ShouldPropagateUncaughtException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), "throw new system.invalidOperationException('boom')"));

            Assert.That(ex?.Message, Is.EqualTo("boom"));
        }
    }
}
