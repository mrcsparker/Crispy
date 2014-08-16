using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Crispy.Tests.Data;
using NUnit.Framework;
using System.Reflection;

namespace Crispy.Tests
{
    [TestFixture]
    public class ProgramTest
    {
        [Test]
        public void ShouldBeAbleToCallCompiler() {
            string dllPath = typeof(object).Assembly.Location;
            Assembly asm = Assembly.LoadFile(dllPath);

            var Crispy = new Crispy(new[] { asm });
            //Crispy.ExecuteCode('1');
        }
    }
}

