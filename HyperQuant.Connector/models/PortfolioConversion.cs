namespace HyperQuant.Connector.models
{
    public class BalanceEntry
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public float Amount { get; set; }
    }

    public class BalanceData
    {
        public List<BalanceEntry> Currencies { get; set; }
    }

    public class PortfolioConversion
    {
        public string Currency { get; set; }
        public float TotalValue { get; set; }
    }
}
