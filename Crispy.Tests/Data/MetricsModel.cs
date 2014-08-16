namespace Crispy.Tests.Data
{
    public class MetricsModel
    {
        public double Id { get; set; }
        public string Name { get; set; }
        public double Sales { get; set; }
        public double Volume { get; set; }
        public double Margin { get; set; }
        public double Profit { get; set; }

        public double GetSalesVolume()
        {
            return Sales*Volume;
        }

        public double GetSales()
        {
            return Sales;
        }

        public double GetVolume()
        {
            return Volume;
        }

        public double GetProfit()
        {
            return Profit;
        }

        public bool ProfitEq(double num)
        {
            return Profit == num;
        }
    }
}
