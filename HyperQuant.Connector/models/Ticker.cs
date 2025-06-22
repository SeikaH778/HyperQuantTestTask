

namespace HyperQuant.Connector.models
{
    public  class Ticker
    {
        public float Bid { get; set; }
        public float BidSize { get; set; }
        public float Ask { get; set; }
        public float AskSize { get; set; }
        public float DailyChange { get; set; }
        public float DailyChangeRelative { get; set; }
        public float LastPrice { get; set; }
        public float Volume { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
    }
}
