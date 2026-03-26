using System;
using System.IO;
using Crispy.Repl;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public sealed class ReplSessionTest
    {
        [Test]
        public void ShouldRetainStateBetweenSubmissions()
        {
            var session = new ReplSession();

            var assign = session.SubmitLine("var x = 40");
            var evaluate = session.SubmitLine("x + 2");

            Assert.That(assign.Kind, Is.EqualTo(ReplSubmissionKind.Executed));
            Assert.That(assign.Value, Is.EqualTo(40));
            Assert.That(evaluate.Kind, Is.EqualTo(ReplSubmissionKind.Executed));
            Assert.That(evaluate.Value, Is.EqualTo(42));
        }

        [Test]
        public void ShouldBufferMultilineFunctionUntilSubmissionIsComplete()
        {
            var session = new ReplSession();

            var start = session.SubmitLine("defun add(a, b) {");
            var body = session.SubmitLine("a + b");
            var end = session.SubmitLine("}");
            var invoke = session.SubmitLine("add(3, 4)");

            Assert.That(start.Kind, Is.EqualTo(ReplSubmissionKind.Incomplete));
            Assert.That(body.Kind, Is.EqualTo(ReplSubmissionKind.Incomplete));
            Assert.That(end.Kind, Is.EqualTo(ReplSubmissionKind.Executed));
            Assert.That(invoke.Kind, Is.EqualTo(ReplSubmissionKind.Executed));
            Assert.That(invoke.Value, Is.EqualTo(7));
        }

        [Test]
        public void ShouldResetSessionState()
        {
            var session = new ReplSession();

            Assert.That(session.SubmitLine("var x = 10").Kind, Is.EqualTo(ReplSubmissionKind.Executed));

            var reset = session.SubmitLine(":reset");
            var evaluate = session.SubmitLine("x");

            Assert.That(reset.Kind, Is.EqualTo(ReplSubmissionKind.Info));
            Assert.That(reset.DisplayText, Is.EqualTo("Session reset."));
            Assert.That(evaluate.Kind, Is.EqualTo(ReplSubmissionKind.Error));
        }

        [Test]
        public void ShouldListSessionBindings()
        {
            var session = new ReplSession();

            Assert.That(session.SubmitLine("var zebra = 1").Kind, Is.EqualTo(ReplSubmissionKind.Executed));
            Assert.That(session.SubmitLine("var alpha = 2").Kind, Is.EqualTo(ReplSubmissionKind.Executed));

            var scope = session.SubmitLine(":scope");

            Assert.That(scope.Kind, Is.EqualTo(ReplSubmissionKind.Info));
            Assert.That(scope.DisplayText, Is.EqualTo("alpha" + Environment.NewLine + "zebra"));
        }

        [Test]
        public void ShouldLoadFileModuleIntoSession()
        {
            var session = new ReplSession();
            var tempDirectory = Path.Combine(Path.GetTempPath(), "Crispy.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var filePath = Path.Combine(tempDirectory, "math.crispy");
                File.WriteAllText(filePath, @"
                    defun add(a, b) {
                        a + b
                    }
                ");

                var load = session.SubmitLine(":load " + filePath);
                var evaluate = session.SubmitLine("math.add(2, 5)");

                Assert.That(load.Kind, Is.EqualTo(ReplSubmissionKind.Info));
                Assert.That(load.DisplayText, Does.Contain("Loaded "));
                Assert.That(load.DisplayText, Does.Contain(" as math."));
                Assert.That(evaluate.Kind, Is.EqualTo(ReplSubmissionKind.Executed));
                Assert.That(evaluate.Value, Is.EqualTo(7));
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}
