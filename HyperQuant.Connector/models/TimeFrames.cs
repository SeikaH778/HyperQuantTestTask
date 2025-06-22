
namespace HyperQuant.Connector.models
{
    public static class TimeFrames
    {
        private static readonly Dictionary<long, string> timeFrames = new() // доступные временные отрезки согласно документации API Bitfinex
        {
            {60, "1m"},
            {300, "5m"},
            {900, "15m"},
            {1800, "30m"},
            {3600, "1h"},
            {10800, "3h"},
            {21600, "6h"},
            {43200, "12h"},
            {86400, "1D"},
            {604800, "1W"},
            {1209600, "14D"},
            {2629744, "1M"}
        };

        public static string? GetTimeframe(long seconds)
        {
            return timeFrames.TryGetValue(seconds, out var value) ? value : null;
        }
        public static IEnumerable<long> SupportedPeriods => timeFrames.Keys;
    }
}

