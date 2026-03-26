using System;
using System.Dynamic;
using System.Reflection;
using Crispy.Parsing;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class DefaultParameterTest
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
        public void ShouldUseFunctionDefaultParameterWhenArgumentIsOmitted()
        {
            var result = Execute(CreateRuntime(), @"
                defun add(a, b = 10) {
                    a + b
                }

                add(5)
            ");

            Assert.AreEqual(15, result);
        }

        [Test]
        public void ShouldPreferExplicitFunctionArgumentOverDefaultValue()
        {
            var result = Execute(CreateRuntime(), @"
                defun add(a, b = 10) {
                    a + b
                }

                add(5, 3)
            ");

            Assert.AreEqual(8, result);
        }

        [Test]
        public void ShouldUseLambdaDefaultParameterWhenArgumentIsOmitted()
        {
            var result = Execute(CreateRuntime(), @"
                var add = lambda(a, b = 4) {
                    a + b
                }

                add(6)
            ");

            Assert.AreEqual(10, result);
        }

        [Test]
        public void ShouldAllowDefaultValuesToReferenceEarlierParameters()
        {
            var result = Execute(CreateRuntime(), @"
                defun score(base, bonus = base + 2) {
                    base + bonus
                }

                score(5)
            ");

            Assert.AreEqual(12, result);
        }

        [Test]
        public void ShouldAllowDefaultValuesToCaptureOuterScope()
        {
            var result = Execute(CreateRuntime(), @"
                defun makeAdder(offset) {
                    lambda(value, extra = offset) {
                        value + extra
                    }
                }

                var add7 = makeAdder(7)
                add7(5)
            ");

            Assert.AreEqual(12, result);
        }

        [Test]
        public void ShouldRejectTooFewArgumentsForDefaultedCallable()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    defun add(a, b = 2, c = 3) {
                        a + b + c
                    }

                    add()
                "));

            Assert.That(ex?.Message, Is.EqualTo("Wrong number of arguments for function -- expected 1 to 3 got 0"));
        }

        [Test]
        public void ShouldRejectTooManyArgumentsForDefaultedCallable()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    defun add(a, b = 2, c = 3) {
                        a + b + c
                    }

                    add(1, 2, 3, 4)
                "));

            Assert.That(ex?.Message, Is.EqualTo("Wrong number of arguments for function -- expected 1 to 3 got 4"));
        }

        [Test]
        public void ShouldRejectRequiredParameterAfterOptionalParameter()
        {
            var ex = Assert.Throws<ParserException>(() =>
                Execute(CreateRuntime(), @"
                    defun bad(a = 1, b) {
                        b
                    }
                "));

            Assert.That(ex?.Message, Is.EqualTo("Required parameters cannot follow optional parameters."));
        }

        [Test]
        public void ShouldRejectVariadicParameters()
        {
            var ex = Assert.Throws<ParserException>(() =>
                Execute(CreateRuntime(), @"
                    defun bad(...rest) {
                        1
                    }
                "));

            Assert.That(ex?.Message, Is.EqualTo("Variadic parameters are not supported."));
        }
    }
}
