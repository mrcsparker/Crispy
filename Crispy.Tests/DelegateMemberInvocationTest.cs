using System;
using System.Dynamic;
using Crispy.Tests.Data;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public sealed class DelegateMemberInvocationTest
    {
        private static CrispyRuntime CreateRuntime()
        {
            return new CrispyRuntime([typeof(object).Assembly, typeof(DelegateHost).Assembly]);
        }

        private static object Execute(CrispyRuntime crispy, string text, ExpandoObject? scope = null)
        {
            return crispy.ExecuteExpr(text, scope ?? new ExpandoObject());
        }

        [Test]
        public void ShouldInvokeInstanceDelegateProperty()
        {
            var result = Execute(CreateRuntime(), @"
                var host = new crispy.tests.data.DelegateHost()
                host.AddProperty(2, 5)
            ");

            Assert.AreEqual(7, result);
        }

        [Test]
        public void ShouldInvokeInstanceDelegateField()
        {
            var result = Execute(CreateRuntime(), @"
                var host = new crispy.tests.data.DelegateHost()
                host.MultiplyField(3, 4)
            ");

            Assert.AreEqual(12, result);
        }

        [Test]
        public void ShouldInvokeInstanceObjectTypedCallableProperty()
        {
            var result = Execute(CreateRuntime(), @"
                var host = new crispy.tests.data.DelegateHost()
                host.ObjectAddProperty(6, 7)
            ");

            Assert.AreEqual(13, result);
        }

        [Test]
        public void ShouldInvokeStaticDelegateProperty()
        {
            var result = Execute(CreateRuntime(), "crispy.tests.data.DelegateHost.StaticAddProperty(1, 8)");

            Assert.AreEqual(9, result);
        }

        [Test]
        public void ShouldInvokeStaticDelegateField()
        {
            var result = Execute(CreateRuntime(), "crispy.tests.data.DelegateHost.StaticMultiplyField(2, 6)");

            Assert.AreEqual(12, result);
        }

        [Test]
        public void ShouldInvokeStaticObjectTypedCallableProperty()
        {
            var result = Execute(CreateRuntime(), "crispy.tests.data.DelegateHost.StaticObjectSubtractProperty(10, 3)");

            Assert.AreEqual(7, result);
        }

        [Test]
        public void ShouldThrowForNonCallableInstancePropertyInvocation()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), @"
                    var host = new crispy.tests.data.DelegateHost()
                    host.LabelProperty()
                "));
        }

        [Test]
        public void ShouldThrowForNonCallableStaticPropertyInvocation()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Execute(CreateRuntime(), "crispy.tests.data.DelegateHost.StaticLabelProperty()"));
        }
    }
}
