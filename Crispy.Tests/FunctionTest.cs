using System.Dynamic;
using System.Reflection;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class FunctionTest
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
        public void ShouldBeAbleToCreateASimpleFunction()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun add(a, b) {
                    a + b
                }

                add(3, 4)
            ");

            Assert.AreEqual(7, output);
        }

        [Test]
        public void ShouldStoreCreatedFunctionInModuleScope()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun add(a, b) {
                    a + b
                }
            ", scope);

            var output = Execute(crispy, "add(10, 5)", scope);

            Assert.AreEqual(15, output);
        }

        [Test]
        public void ShouldBeAbleToCreateFunctionWithNoArguments()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun answer() {
                    15
                }

                answer()
            ");

            Assert.AreEqual(15, output);
        }

        [Test]
        public void ShouldBeAbleToCreateFunctionThatUsesLocalVariables()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun total(a, b, c) {
                    var partial = a + b
                    partial + c
                }

                total(1, 2, 3)
            ");

            Assert.AreEqual(6, output);
        }

        [Test]
        public void ShouldBeAbleToCreateFunctionThatCallsAnotherFunction()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun add(a, b) {
                    a + b
                }

                defun doubleValue(x) {
                    add(x, x)
                }

                doubleValue(6)
            ");

            Assert.AreEqual(12, output);
        }

        [Test]
        public void ShouldBeAbleToTreatFunctionAsFirstClassValue()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun add(a, b) {
                    a + b
                }

                var fn = add
                fn(2, 5)
            ");

            Assert.AreEqual(7, output);
        }

        [Test]
        public void ShouldBeAbleToPassLambdaToFunction()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun apply(fn, value) {
                    fn(value)
                }

                apply(lambda(x) { x * 3 }, 4)
            ");

            Assert.AreEqual(12, output);
        }

        [Test]
        public void ShouldBeAbleToReturnEarlyFromFunction()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun classify(x) {
                    if (x > 0) {
                        return 1
                    }

                    return 0
                }
            ", scope);

            var positive = Execute(crispy, "classify(5)", scope);
            var zero = Execute(crispy, "classify(0)", scope);

            Assert.AreEqual(1, positive);
            Assert.AreEqual(0, zero);
        }

        [Test]
        public void ShouldBeAbleToUseModuleVariablesInsideFunction()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                var offset = 2

                defun addOffset(value) {
                    value + offset
                }

                addOffset(5)
            ");

            Assert.AreEqual(7, output);
        }

        [Test]
        public void ShouldBeAbleToCallFunctionRecursively()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun countdown(n) {
                    if (n <= 1) {
                        return 1
                    }

                    countdown(n - 1)
                }

                countdown(5)
            ");

            Assert.AreEqual(1, output);
        }

        [Test]
        public void ShouldPreserveCapturedTopLevelVariablesAcrossCalls()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                var offset = 7

                defun addOffset(value) {
                    value + offset
                }
            ", scope);

            var output = Execute(crispy, "addOffset(5)", scope);

            Assert.AreEqual(12, output);
        }

        [Test]
        public void ShouldAllowLoopLocalsToEscapeThroughClosures()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                var getter = lambda() { 0 }

                loop {
                    var hidden = 41
                    getter = lambda() { hidden + 1 }
                    break
                }

                getter()
            ");

            Assert.AreEqual(42, output);
        }

        [Test]
        public void ShouldCaptureFreshLoopLocalPerIteration()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                var first = lambda() { 0 }
                var second = lambda() { 0 }
                var index = 0

                loop {
                    var captured = index

                    if (index == 0) {
                        first = lambda() { captured }
                        index = index + 1
                    } else {
                        second = lambda() { captured }
                        break
                    }
                }

                first() + second()
            ");

            Assert.AreEqual(1, output);
        }

        [Test]
        public void ShouldAllowNestedFunctionToCaptureOuterVariables()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun makeAdder(offset) {
                    defun add(value) {
                        value + offset
                    }

                    add(5)
                }

                makeAdder(7)
            ");

            Assert.AreEqual(12, output);
        }

        [Test]
        public void ShouldAllowNestedFunctionToCallItselfRecursively()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun countdownFrom(n) {
                    defun inner(value) {
                        if (value <= 1) {
                            1
                        } else {
                            inner(value - 1)
                        }
                    }

                    inner(n)
                }

                countdownFrom(5)
            ");

            Assert.AreEqual(1, output);
        }

        [Test]
        public void ShouldKeepNestedFunctionOutOfModuleScope()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun outer() {
                    defun inner() {
                        42
                    }

                    inner()
                }
            ", scope);

            var result = Execute(crispy, "outer()", scope);
            var module = (System.Collections.Generic.IDictionary<string, object?>)scope;

            Assert.AreEqual(42, result);
            Assert.IsFalse(module.ContainsKey("inner"));
        }

        [Test]
        public void ShouldImplicitlyReturnLastExpressionWhenAnotherBranchReturnsEarly()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun classify(x) {
                    if (x > 0) {
                        return 1
                    }

                    x + 10
                }
            ", scope);

            var positive = Execute(crispy, "classify(5)", scope);
            var nonPositive = Execute(crispy, "classify(0)", scope);

            Assert.AreEqual(1, positive);
            Assert.AreEqual(10, nonPositive);
        }

        [Test]
        public void ShouldImplicitlyReturnLastLambdaExpressionWhenAnotherBranchReturnsEarly()
        {
            var crispy = CreateRuntime();

            var output = Execute(crispy, @"
                defun apply(fn, value) {
                    fn(value)
                }

                apply(lambda(x) {
                    if (x > 0) {
                        return x
                    }

                    x + 10
                }, 0)
            ");

            Assert.AreEqual(10, output);
        }

        [Test]
        public void ShouldKeepFunctionLocalsIsolatedBetweenCalls()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun incrementBase(baseValue) {
                    var total = baseValue
                    total = total + 1
                    total
                }
            ", scope);

            var first = Execute(crispy, "incrementBase(3)", scope);
            var second = Execute(crispy, "incrementBase(10)", scope);

            Assert.AreEqual(4, first);
            Assert.AreEqual(11, second);
        }

        [Test]
        public void ShouldAllowNullInVariablesAndReturns()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun maybeMissing(flag) {
                    if (flag) {
                        'value'
                    } else {
                        null
                    }
                }
            ", scope);

            var result = Execute(crispy, "maybeMissing(false)", scope);

            Assert.IsNull(result);
        }

        [Test]
        public void ShouldAllowAssigningNullToMembersAndIndexes()
        {
            var crispy = CreateRuntime(
                typeof(System.Dynamic.ExpandoObject).Assembly,
                typeof(System.Collections.ArrayList).Assembly);

            var result = Execute(crispy, @"
                var obj = new system.dynamic.ExpandoObject()
                obj.Value = 'set'

                var items = new system.collections.ArrayList()
                items.Add('set')

                obj.Value = null
                items[0] = null

                (obj.Value == null) and (items[0] == null)
            ");

            Assert.AreEqual(true, result);
        }

        [Test]
        public void ShouldAllowLogicalOrAcrossDynamicComparisons()
        {
            var crispy = CreateRuntime(
                typeof(System.Dynamic.ExpandoObject).Assembly,
                typeof(System.Collections.ArrayList).Assembly);

            var result = Execute(crispy, @"
                var obj = new system.dynamic.ExpandoObject()
                obj.Value = null

                var items = new system.collections.ArrayList()
                items.Add('set')

                (obj.Value == 'set') or (items[0] == 'set')
            ");

            Assert.AreEqual(true, result);
        }

        [Test]
        public void ShouldTreatParenthesizedExpressionOnNextLineAsANewStatement()
        {
            var crispy = CreateRuntime(typeof(System.Collections.ArrayList).Assembly);

            var result = Execute(crispy, @"
                var items = new system.collections.ArrayList()
                items.Add('set')

                (1 + 1)
            ");

            Assert.AreEqual(2, result);
        }
    }
}
