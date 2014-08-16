using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Tests.Data;
using NUnit.Framework;
using System.Dynamic;

namespace Crispy.Tests
{
    [TestFixture]
    class ExternalObjectInjectionTest
    {
        private readonly Crispy _Crispy;
        private readonly Product _product;

        private static IEnumerable<Product> GetProductList()
        {
            var products = new List<Product>
            {
                new Product {Name = "Item1", Price = 1, Volume = 1},
                new Product {Name = "Item2", Price = 2, Volume = 2},
                new Product {Name = "Item3", Price = 3, Volume = 3},
                new Product {Name = "Item4", Price = 4, Volume = 4},
                new Product {Name = "Item5", Price = 5, Volume = 5},
                new Product {Name = "Item6", Price = 6, Volume = 6},
                new Product {Name = "Item7", Price = 7, Volume = 7},
                new Product {Name = "Item8", Price = 8, Volume = 8},
                new Product {Name = "Item9", Price = 9, Volume = 9},
                new Product {Name = "Item10", Price = 10, Volume = 10}
            };

            foreach (Product product in products)
            {
                product.Products = products;
            }

            return products;
        }

        public ExternalObjectInjectionTest()
        {
            string dllPath = typeof(object).Assembly.Location;
            Assembly asm = Assembly.LoadFile(dllPath);

            IEnumerable<Product> productList = GetProductList();
            _product = productList.First();

            _Crispy = new Crispy(new[] { asm }, new [] { _product });
        }

        public object Exec(string text)
        {
            return _Crispy.ExecuteExpr(text, new ExpandoObject());
        }

        [Test]
        public void ShouldBeAbleToBindInstanceToParser()
        {
            string productNameLower = _product.Name.ToLower();

            var output = Exec("LowerCaseName()");

            Assert.AreEqual(output, productNameLower);
        }

        [Test]
        public void ShouldBeAbleToCallInstanceOfObjectWithExpressions()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();
            string productNameLower = product.Name.ToLower();

            MethodInfo method = typeof (Product).GetMethod("LowerCaseName");

            MethodCallExpression expression = Expression.Call(Expression.Constant(product), method);
            Func<string> output = Expression.Lambda<Func<string>>(expression).Compile();

            Assert.AreEqual(output.Invoke(), productNameLower);
        }

        [Test]
        public void ShouldLoadProductList()
        {
            IEnumerable<Product> productList = GetProductList();
            Assert.AreEqual(10, productList.Count());
        }
    }
}
