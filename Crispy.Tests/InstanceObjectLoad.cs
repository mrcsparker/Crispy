using System.Dynamic;
using System.Reflection;
using Crispy.Tests.Data;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class InstanceObjectLoad
    {

        private readonly CrispyRuntime _Crispy;

        private static MetricsModel MakeSimpleMetricsModel()
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
            Assembly asm = typeof(object).Assembly;

            var metricsModel = MakeSimpleMetricsModel();

            _Crispy = new CrispyRuntime(new[] { asm }, new[] { metricsModel });
        }

        public object Exec(string text)
        {
            return _Crispy.ExecuteExpr(text, new ExpandoObject());
        }

        [Test]
        public void ShouldBeAbleToPlugInAModel()
        {
            double r = (double)Exec("ReadSales()");
            Assert.AreEqual(198, r);

            r = (double)Exec("ReadVolume()");
            Assert.AreEqual(122, r);
        }

        [Test]
        public void ShouldBeAbleToPlugInAModelAndCompare()
        {
            bool r = (bool)Exec("MetricsModel.ReadVolume() > 1.0");
            Assert.IsTrue(r);

            r = (bool)Exec("1.0 < ReadVolume()");
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
