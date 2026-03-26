using System.Collections;
using System.Dynamic;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class LiteralTest
    {
        private static CrispyRuntime CreateRuntime()
        {
            return new CrispyRuntime([typeof(object).Assembly]);
        }

        private static object Execute(CrispyRuntime crispy, string text, ExpandoObject? scope = null)
        {
            return crispy.ExecuteExpr(text, scope ?? new ExpandoObject());
        }

        [Test]
        public void ShouldCreateEmptyListLiteral()
        {
            var result = Execute(CreateRuntime(), "[]");

            Assert.That(result, Is.TypeOf<ArrayList>());
            Assert.AreEqual(0, ((ArrayList)result).Count);
        }

        [Test]
        public void ShouldCreateEmptyDictionaryLiteral()
        {
            var result = Execute(CreateRuntime(), "dict[]");

            Assert.That(result, Is.TypeOf<Hashtable>());
            Assert.AreEqual(0, ((Hashtable)result).Count);
        }

        [Test]
        public void ShouldSupportNestedAndMixedLiterals()
        {
            var result = Execute(CreateRuntime(), @"
                var data = dict[
                    'numbers': [1, 'two', null],
                    'meta': dict['active': true, 'items': [10, 20]]
                ]

                data['meta']['items'][1]
            ");

            Assert.AreEqual(20, result);
        }

        [Test]
        public void ShouldAllowMutatingLiteralCreatedCollections()
        {
            var result = Execute(CreateRuntime(), @"
                var values = [1, 2, 3]
                var lookup = dict['a': 1]

                values[1] = 20
                lookup['b'] = values[1]

                lookup['b'] + values[2]
            ");

            Assert.AreEqual(23, result);
        }

        [Test]
        public void ShouldAllowLiteralValuesInClosuresFunctionCallsAndReturns()
        {
            var crispy = CreateRuntime();
            var scope = new ExpandoObject();

            Execute(crispy, @"
                defun makeTracker(seed) {
                    var state = dict['count': seed, 'history': []]

                    lambda() {
                        state['count'] = state['count'] + 1
                        state['history'].Add(state['count'])
                        state['history'][state['history'].Count - 1]
                    }
                }
            ", scope);

            var result = Execute(crispy, @"
                var next = makeTracker(0)
                next() + next()
            ", scope);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void ShouldInteropWithDotNetMethodsOnLiteralCreatedCollections()
        {
            var result = Execute(CreateRuntime(), @"
                var values = [1, 2]
                values.Add(3)

                var lookup = dict['x': 1]

                lookup.ContainsKey('x') and (values.Count == 3)
            ");

            Assert.AreEqual(true, result);
        }
    }
}
