using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class ProgramTest
    {
        private static CrispyRuntime CreateRuntime(params Assembly[] extraAssemblies)
        {
            return new CrispyRuntime([typeof(object).Assembly, .. extraAssemblies]);
        }

        private static void InTempDirectory(Action<string> test)
        {
            var tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "Crispy.Tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempDirectory);
            try
            {
                test(tempDirectory);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        private static string WriteFile(string directory, string fileName, string contents)
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        [Test]
        public void ShouldExposeExecutedFileUsingBaseFileName()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                var modulePath = WriteFile(tempDirectory, "mathmodule.sympl", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                crispy.ExecuteFile(modulePath);

                var output = crispy.ExecuteExpr("mathmodule.add(2, 3)", new ExpandoObject());

                Assert.AreEqual(5, output);
            });
        }

        [Test]
        public void ShouldSupportLegacyRuntimeTypeName()
        {
            var crispy = new global::Crispy.Crispy([typeof(object).Assembly]);

            var output = crispy.ExecuteExpr("1 + 2", new ExpandoObject());

            Assert.AreEqual(3, output);
        }

        [Test]
        public void ShouldExposeExecutedFileUsingCustomGlobalAlias()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                var modulePath = WriteFile(tempDirectory, "calculator.sympl", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var moduleScope = crispy.ExecuteFile(modulePath, "helpers");
                var globals = (IDictionary<string, object?>)crispy.Globals;
                var output = crispy.ExecuteExpr("helpers.add(4, 1)", new ExpandoObject());

                Assert.That(globals["helpers"], Is.SameAs(moduleScope));
                Assert.AreEqual(5, output);
            });
        }

        [Test]
        public void ShouldExecuteFileInProvidedScopeWithoutRegisteringGlobalModule()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                var modulePath = WriteFile(tempDirectory, "scopedmodule.sympl", @"
                    defun answer() {
                        40 + 2
                    }
                ");

                var moduleScope = new ExpandoObject();
                crispy.ExecuteFileInScope(modulePath, moduleScope);

                var scope = (IDictionary<string, object?>)moduleScope;
                var globals = (IDictionary<string, object?>)crispy.Globals;
                var output = crispy.ExecuteExpr("answer()", moduleScope);

                Assert.AreEqual(42, output);
                Assert.AreEqual(Path.GetFullPath(modulePath), scope["__file__"]);
                Assert.IsTrue(scope.ContainsKey("answer"));
                Assert.IsFalse(globals.ContainsKey("scopedmodule"));
            });
        }

        [Test]
        public void ShouldImportSiblingFileIntoModuleScope()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                WriteFile(tempDirectory, "math.sympl", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var programPath = WriteFile(tempDirectory, "program.sympl", @"
                    import math as calc

                    defun run() {
                        calc.add(2, 5)
                    }
                ");

                var programScope = crispy.ExecuteFile(programPath);
                var scope = (IDictionary<string, object?>)programScope;
                var output = crispy.ExecuteExpr("run()", programScope);

                Assert.AreEqual(7, output);
                Assert.IsTrue(scope.ContainsKey("calc"));
                Assert.IsTrue(scope.ContainsKey("run"));
            });
        }

        [Test]
        public void ShouldImportSiblingCrispyFileIntoModuleScope()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                WriteFile(tempDirectory, "math.crispy", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var programPath = WriteFile(tempDirectory, "program.crispy", @"
                    import math as calc

                    defun run() {
                        calc.add(8, 5)
                    }
                ");

                var programScope = crispy.ExecuteFile(programPath);
                var scope = (IDictionary<string, object?>)programScope;
                var output = crispy.ExecuteExpr("run()", programScope);

                Assert.AreEqual(13, output);
                Assert.IsTrue(scope.ContainsKey("calc"));
                Assert.IsTrue(scope.ContainsKey("run"));
            });
        }

        [Test]
        public void ShouldImportSiblingFileIntoFunctionScopeWithoutLeakingAliasToModule()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                WriteFile(tempDirectory, "math.crispy", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var programPath = WriteFile(tempDirectory, "program.crispy", @"
                    defun run() {
                        import math as calc
                        calc.add(4, 9)
                    }
                ");

                var programScope = crispy.ExecuteFile(programPath);
                var scope = (IDictionary<string, object?>)programScope;
                var output = crispy.ExecuteExpr("run()", programScope);

                Assert.AreEqual(13, output);
                Assert.IsFalse(scope.ContainsKey("calc"));
                Assert.IsTrue(scope.ContainsKey("run"));
            });
        }

        [Test]
        public void ShouldCaptureFunctionScopedImportInReturnedClosure()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                WriteFile(tempDirectory, "math.crispy", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var programPath = WriteFile(tempDirectory, "program.crispy", @"
                    defun makeAdder(offset) {
                        import math as calc

                        lambda(value) {
                            calc.add(value, offset)
                        }
                    }

                    defun run() {
                        var add5 = makeAdder(5)
                        add5(2)
                    }
                ");

                var programScope = crispy.ExecuteFile(programPath);
                var scope = (IDictionary<string, object?>)programScope;
                var output = crispy.ExecuteExpr("run()", programScope);

                Assert.AreEqual(7, output);
                Assert.IsFalse(scope.ContainsKey("calc"));
                Assert.IsTrue(scope.ContainsKey("makeAdder"));
            });
        }

        [Test]
        public void ShouldKeepNestedBlockImportsLexicallyScoped()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                WriteFile(tempDirectory, "math.crispy", @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var programPath = WriteFile(tempDirectory, "program.crispy", @"
                    defun run() {
                        var calc = 1
                        var fromBlock = 0

                        if (true) {
                            import math as calc
                            fromBlock = calc.add(2, 3)
                        }

                        fromBlock + calc
                    }
                ");

                var programScope = crispy.ExecuteFile(programPath);
                var scope = (IDictionary<string, object?>)programScope;
                var output = crispy.ExecuteExpr("run()", programScope);

                Assert.AreEqual(6, output);
                Assert.IsFalse(scope.ContainsKey("calc"));
            });
        }

        [Test]
        public void ShouldReuseSiblingFileModuleWhenImportedRepeatedlyInLocalScope()
        {
            var crispy = CreateRuntime();

            InTempDirectory(tempDirectory =>
            {
                WriteFile(tempDirectory, "stateful.crispy", @"
                    var token = system.guid.NewGuid().ToString()

                    defun getToken() {
                        token
                    }
                ");

                var programPath = WriteFile(tempDirectory, "program.crispy", @"
                    defun run() {
                        import stateful as current
                        var firstToken = current.getToken()

                        import stateful as current
                        var secondToken = current.getToken()

                        firstToken == secondToken
                    }
                ");

                var programScope = crispy.ExecuteFile(programPath);
                var scope = (IDictionary<string, object?>)programScope;
                var output = crispy.ExecuteExpr("run()", programScope);

                Assert.AreEqual(true, output);
                Assert.IsFalse(scope.ContainsKey("current"));
            });
        }
    }
}
