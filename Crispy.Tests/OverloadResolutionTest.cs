using System;
using System.Dynamic;
using Crispy.Tests.Data;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public sealed class OverloadResolutionTest
    {
        private static CrispyRuntime CreateRuntime()
        {
            return new CrispyRuntime([typeof(object).Assembly, typeof(OverloadHost).Assembly]);
        }

        private static object Execute(CrispyRuntime crispy, string text, ExpandoObject? scope = null)
        {
            return crispy.ExecuteExpr(text, scope ?? new ExpandoObject());
        }

        [Test]
        public void ShouldChooseExactOverloadedInstanceMethod()
        {
            var result = Execute(CreateRuntime(), @"
                var host = new crispy.tests.data.OverloadHost(0)
                host.Select(4.5)
            ");

            Assert.AreEqual("double", result);
        }

        [Test]
        public void ShouldChooseExactOverloadedStaticMethod()
        {
            var result = Execute(CreateRuntime(), "crispy.tests.data.OverloadHost.StaticSelect(4)");

            Assert.AreEqual("int", result);
        }

        [Test]
        public void ShouldPreferSmallerNumericWideningForStaticMethod()
        {
            var result = Execute(CreateRuntime(), "crispy.tests.data.OverloadHost.StaticSelect(system.int16.Parse('3'))");

            Assert.AreEqual("int", result);
        }

        [Test]
        public void ShouldPreferSmallerNumericWideningForInstanceMethod()
        {
            var result = Execute(CreateRuntime(), @"
                var host = new crispy.tests.data.OverloadHost(0)
                host.Widen(system.int16.Parse('3'))
            ");

            Assert.AreEqual("int", result);
        }

        [Test]
        public void ShouldPreferSmallerNumericWideningForConstructor()
        {
            var result = Execute(CreateRuntime(), @"
                var host = new crispy.tests.data.OverloadHost(system.int16.Parse('3'))
                host.Kind
            ");

            Assert.AreEqual("int", result);
        }

        [Test]
        public void ShouldReportAmbiguousOverloadClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    var host = new crispy.tests.data.OverloadHost(0)
                    host.Ambiguous(1)
                "));

            Assert.That(ex?.Message, Does.StartWith("Ambiguous overload for member 'Ambiguous': "));
            Assert.That(ex?.Message, Does.Contain("System.IConvertible"));
            Assert.That(ex?.Message, Does.Contain("System.IFormattable"));
        }

        [Test]
        public void ShouldRejectOmittedOptionalMethodArgumentsClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    var host = new crispy.tests.data.OverloadHost(0)
                    host.OptionalInstance(5)
                "));

            Assert.That(ex?.Message, Is.EqualTo(
                "Optional parameters are not supported for member 'OptionalInstance'; pass all arguments explicitly."));
        }

        [Test]
        public void ShouldRejectOmittedOptionalStaticMethodArgumentsClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), "crispy.tests.data.OverloadHost.StaticOptional(5)"));

            Assert.That(ex?.Message, Is.EqualTo(
                "Optional parameters are not supported for member 'StaticOptional'; pass all arguments explicitly."));
        }

        [Test]
        public void ShouldRejectOmittedOptionalConstructorArgumentsClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), "new crispy.tests.data.OptionalConstructorHost(5)"));

            Assert.That(ex?.Message, Is.EqualTo(
                "Optional parameters are not supported for constructor 'Crispy.Tests.Data.OptionalConstructorHost'; pass all arguments explicitly."));
        }

        [Test]
        public void ShouldRejectGenericMethodsClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    var host = new crispy.tests.data.OverloadHost(0)
                    host.Generic(5)
                "));

            Assert.That(ex?.Message, Is.EqualTo("Generic methods are not supported for member 'Generic'."));
        }

        [Test]
        public void ShouldRejectStaticGenericMethodsClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), "crispy.tests.data.OverloadHost.StaticGeneric(5)"));

            Assert.That(ex?.Message, Is.EqualTo("Generic methods are not supported for member 'StaticGeneric'."));
        }

        [Test]
        public void ShouldRejectRefOutMethodsClearly()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    var host = new crispy.tests.data.OverloadHost(0)
                    host.RefOnly(5)
                "));

            Assert.That(ex?.Message, Is.EqualTo("ref/out parameters are not supported for member 'RefOnly'."));
        }
    }
}
