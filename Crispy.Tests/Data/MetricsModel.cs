namespace Crispy.Tests.Data
{
    internal sealed class MetricsModel
    {
        public double Id { get; set; }
        public string? Name { get; set; }
        public double Sales { get; set; }
        public double Volume { get; set; }
        public double Margin { get; set; }
        public double Profit { get; set; }

        public double SalesVolume()
        {
            return Sales * Volume;
        }

        public double ReadSales()
        {
            return Sales;
        }

        public double ReadVolume()
        {
            return Volume;
        }

        public double ReadProfit()
        {
            return Profit;
        }

        public bool ProfitEq(double num)
        {
            return Profit == num;
        }
    }
}
