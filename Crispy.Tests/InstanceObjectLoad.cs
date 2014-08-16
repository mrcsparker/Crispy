using Crispy.Tests.Data;
using NUnit.Framework;
using System.Reflection;
using System.Dynamic;
using System;

namespace Crispy.Tests
{
    [TestFixture]
    public class InstanceObjectLoad
    {

        private readonly Crispy _Crispy;

        public MetricsModel MakeSimpleMetricsModel()
        {
            return new MetricsModel
                {
                    Id = 1,
                    Name = "Foo",
                    Sales = 198,
                    Volume = 122,
                    Margin = 31,
                    Profit = 31
                };
        }

        public InstanceObjectLoad()
        {
            string dllPath = typeof(object).Assembly.Location;
            Assembly asm = Assembly.LoadFile(dllPath);

            var metricsModel = MakeSimpleMetricsModel();

            _Crispy = new Crispy(new[] { asm }, new [] { metricsModel });
        }

        public object Exec(string text)
        {
            return _Crispy.ExecuteExpr(text, new ExpandoObject());
        }

        [Test]
        public void ShouldBeAbleToPlugInAModel()
        {
            double r = (double) Exec("GetSales()");
            Assert.AreEqual(198, r);

            r = (double) Exec("GetVolume()");
            Assert.AreEqual(122, r);
        }

        [Test]
        public void ShouldBeAbleToPlugInAModelAndCompare()
        {
            bool r = (bool) Exec("MetricsModel.GetVolume() > 1.0");
            Assert.IsTrue(r);

            r = (bool) Exec("1.0 < GetVolume()");
            Assert.IsTrue(r);
        }

        [Test]
        public void ShouldBeAbleToHandleMethodsWithArguments()
        {
            bool r = (bool)Exec("ProfitEq(31)");
            Assert.IsTrue(r);
        }
    }
}
