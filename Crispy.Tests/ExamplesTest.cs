using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class ExamplesTest
    {
        private static readonly Assembly[] RuntimeAssemblies =
        [
            typeof(object).Assembly,
            typeof(System.Text.StringBuilder).Assembly,
            typeof(System.Dynamic.ExpandoObject).Assembly
        ];

        private static readonly string[] ExpectedExtensions = [".crispy"];

        private static IEnumerable<TestCaseData> ExampleCases()
        {
            yield return new TestCaseData("factorial.crispy", 720);
            yield return new TestCaseData("fibonacci.crispy", 55);
            yield return new TestCaseData("gcd.crispy", 21);
            yield return new TestCaseData("bitwise.crispy", 28);
            yield return new TestCaseData("counter_factory.crispy", 21);
            yield return new TestCaseData("compose.crispy", 41);
            yield return new TestCaseData("defaults.crispy", 67);
            yield return new TestCaseData("dynamic_card.crispy", "[[DLR]] by Crispy: dynamic, closures");
            yield return new TestCaseData("exceptions.crispy", "boom / caught!");
            yield return new TestCaseData("foreach_totals.crispy", 13);
            yield return new TestCaseData("literals.crispy", 34);
            yield return new TestCaseData("memoized_fibonacci.crispy", 144);
            yield return new TestCaseData("pipeline.crispy", 220);
            yield return new TestCaseData("scoreboard.crispy", "Crispy: 10 points, 1 wins");
            yield return new TestCaseData("scoped_imports.crispy", "[[scoped]] -> [[imports]]");
            yield return new TestCaseData("text_tools.crispy", "[[helpers]]");
            yield return new TestCaseData("word_operators.crispy", 8);
            yield return new TestCaseData(
                "collatz.crispy",
                "19 -> 58 -> 29 -> 88 -> 44 -> 22 -> 11 -> 34 -> 17 -> 52 -> 26 -> 13 -> 40 -> 20 -> 10 -> 5 -> 16 -> 8 -> 4 -> 2 -> 1");
            yield return new TestCaseData(
                "fizzbuzz.crispy",
                "1, 2, Fizz, 4, Buzz, Fizz, 7, 8, Fizz, Buzz, 11, Fizz, 13, 14, FizzBuzz, 16, 17, Fizz, 19, Buzz");
            yield return new TestCaseData(
                "pyramid.crispy",
                string.Join(
                    Environment.NewLine,
                    "   *",
                    "  ***",
                    " *****",
                    "*******") + Environment.NewLine);
        }

        private static string ExamplesDirectory =>
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "examples"));

        [TestCaseSource(nameof(ExampleCases))]
        public void ShouldExecuteExampleAndReturnExpectedValue(string fileName, object expected)
        {
            var crispy = new CrispyRuntime(RuntimeAssemblies);
            var filePath = Path.Combine(ExamplesDirectory, fileName);

            var moduleScope = crispy.ExecuteFile(filePath);
            var result = crispy.ExecuteExpr("run()", moduleScope);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ShouldOnlyContainCrispyExampleScripts()
        {
            var exampleFiles = Directory.GetFiles(ExamplesDirectory)
                .Select(Path.GetExtension)
                .Distinct()
                .OrderBy(extension => extension)
                .ToArray();

            Assert.That(exampleFiles, Is.EqualTo(ExpectedExtensions));
        }
    }
}
