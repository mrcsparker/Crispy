using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Crispy.Tests.Data;
using NUnit.Framework;

namespace Crispy.Tests
{
    [TestFixture]
    public class IfThenTest
    {
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

        [Test]
        public void ShouldBeAbleToRunElseIf()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (2 == 1) then
                    AddOutput('Two Equals one.')
                elseif (1 == 1) then
                    AddOutput('One Equals one.')
                else
                    AddOutput('Should not be hit.')
                endif
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "One Equals one.");
        }

        [Test]
        public void ShouldBeAbleToRunIf()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (1 == 1) then 
                    AddOutput('This is data')
                endif";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "This is data");
        }

        [Test]
        public void ShouldBeAbleToRunIfKeyword()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (1 == 1) then 
                    AddOutput('This is data')
                endif";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "This is data");
        }

        [Test]
        public void ShouldBeAbleToRunIfThenElse()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (1 == 2) then
                    AddOutput('no');
                else
                    AddOutput('yes');
                end
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "yes");
        }

        [Test]
        public void ShouldBeAbleToRunNestedIfKeyword()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (1 == 1) then
                    AddOutput('One Equals one.')

                    if (2 > 1) then
                        AddOutput('Two is greater than one.')
                    endif

                    if (1 > 2) then
                        AddOutput('One is greater than two.')
                    endif
                endif
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "One Equals one.Two is greater than one.");
        }

        [Test]
        public void ShouldBeAbleToRunNestedIfKeywordFive()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (1 == 1 and 2 > 1 and ((300 - 200) == 100)) then
                    AddOutput('One Equals one.')

                    if (2 > 1) then
                        AddOutput('Two is greater than one.')
                    endif
                endif
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "One Equals one.Two is greater than one.");
        }

        [Test]
        public void ShouldBeAbleToRunNestedIfKeywordFour()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (1 == 1) then
                    AddOutput('One Equals one.')

                    if (2 > 1) then
                        AddOutput('Two is greater than one.')
                    endif
                endif
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "One Equals one.Two is greater than one.");
        }

        [Test]
        public void ShouldBeAbleToRunNestedIfKeywordThree()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.Last();

            const string text = @"
                if (Top3Price()) then
                    AddOutput('Top 3 price')

                    if (2 > 1) then
                        AddOutput('Two is greater than one.')
                    endif

                    if (1 > 2) then
                        AddOutput('One is greater than two.')
                    endif
                endif
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), "Top 3 priceTwo is greater than one.");
        }

        [Test]
        public void ShouldBeAbleToRunNestedIfKeywordTwo()
        {
            IEnumerable<Product> productList = GetProductList();
            Product product = productList.First();

            const string text = @"
                if (2 == 1) then
                    AddOutput('One Equals one.')

                    if (2 > 1) then
                        AddOutput('Two is greater than one.')
                    endif

                    if (1 > 2) then
                        AddOutput('One is greater than two.')
                    endif
                endif
            ";

            Execute(text, product);

            Assert.AreEqual(product.GetOutput(), string.Empty);
        }

        private static void Execute(string text, Product product)
        {
            var crispy = new CrispyRuntime(new[] { typeof(object).Assembly }, new object[] { product });
            crispy.ExecuteExpr(text, new ExpandoObject());
        }
    }
}
