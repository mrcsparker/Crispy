using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Tests.Data;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public sealed class ExternalObjectInjectionTest
    {
        private readonly CrispyRuntime _Crispy;
        private readonly Product _product;

        private static List<Product> GetProductList()
        {
            var products = new List<Product>
            {
                new() {Name = "Item1", Price = 1, Volume = 1},
                new() {Name = "Item2", Price = 2, Volume = 2},
                new() {Name = "Item3", Price = 3, Volume = 3},
                new() {Name = "Item4", Price = 4, Volume = 4},
                new() {Name = "Item5", Price = 5, Volume = 5},
                new() {Name = "Item6", Price = 6, Volume = 6},
                new() {Name = "Item7", Price = 7, Volume = 7},
                new() {Name = "Item8", Price = 8, Volume = 8},
                new() {Name = "Item9", Price = 9, Volume = 9},
                new() {Name = "Item10", Price = 10, Volume = 10}
            };

            foreach (Product product in products)
            {
                product.Products = products;
            }

            return products;
        }

        public ExternalObjectInjectionTest()
        {
            Assembly asm = typeof(object).Assembly;

            IEnumerable<Product> productList = GetProductList();
            _product = productList.First();

            _Crispy = new CrispyRuntime(new[] { asm }, new[] { _product });
        }

        public object Exec(string text)
        {
            return _Crispy.ExecuteExpr(text, new ExpandoObject());
        }

        [Test]
        public void ShouldBeAbleToBindInstanceToParser()
        {
            string productNameUpper = _product.Name.ToUpperInvariant();

            var output = Exec("UpperCaseName()");

            Assert.AreEqual(output, productNameUpper);
        }

        [Test]
        public void ShouldBeAbleToCallInstanceOfObjectWithExpressions()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();
            string productNameUpper = product.Name.ToUpperInvariant();

            MethodInfo? method = typeof(Product).GetMethod("UpperCaseName");
            Assert.NotNull(method);

            MethodCallExpression expression = Expression.Call(Expression.Constant(product), method!);
            Func<string> output = Expression.Lambda<Func<string>>(expression).Compile();

            Assert.AreEqual(output.Invoke(), productNameUpper);
        }

        [Test]
        public void ShouldLoadProductList()
        {
            IEnumerable<Product> productList = GetProductList();
            Assert.AreEqual(10, productList.Count());
        }
    }
}
