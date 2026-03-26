using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crispy.Tests.Data
{
    internal sealed class Product
    {
        private readonly StringBuilder _output = new StringBuilder();
        public String Name { get; set; } = string.Empty;
        public Double Price { get; set; }
        public Double Volume { get; set; }
        public List<Product> Products { get; set; } = [];

        public string UpperCaseName()
        {
            return Name.ToUpperInvariant();
        }

        public bool Top3Price()
        {
            IEnumerable<Product> topNumbers =
                Products.OrderByDescending(c => c.Price).Take(3);

            return topNumbers.Contains(this);
        }

        public bool Top3Volume()
        {
            IEnumerable<Product> topNumbers =
                Products.OrderByDescending(c => c.Volume).Take(3);

            return topNumbers.Contains(this);
        }

        public void AddOutput(string str)
        {
            _output.Append(str);
        }

        public string GetOutput()
        {
            return _output.ToString();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
