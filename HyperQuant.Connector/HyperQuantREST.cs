using HyperQuant.Connector.models;
using System.Text.Json;

namespace HyperQuant.Connector
{
    public class HyperQuantREST
    {
        private readonly HttpClient httpClient;
        private const string bitFinexRESTApiURL = "https://api-pub.bitfinex.com/v2/";
        public HyperQuantREST()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(bitFinexRESTApiURL)
            };
        }
        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            using (var apiResponse = await httpClient.GetAsync($"trades/{pair}/hist?limit={maxCount}"))
            {
                apiResponse.EnsureSuccessStatusCode();
                string json = await apiResponse.Content.ReadAsStringAsync();

                List<JsonElement> raw = JsonSerializer.Deserialize<List<JsonElement>>(json);
                var result = raw.Select(entry => new Trade
                {
                    Id = entry[0].ToString(),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(entry[1].GetInt64()).DateTime,
                    Amount = entry[2].GetDecimal(),
                    Price = entry[3].GetDecimal(),
                    Pair = pair
                });
                return result;
            }
        }
        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to, long? count)
        {
            HttpResponseMessage apiResponse;
            string? timeframe = TimeFrames.GetTimeframe(periodInSec);
            if (from != null && to !=null)
            {
                apiResponse = await httpClient.GetAsync($"candles/trade:{timeframe}:{pair}/hist?start={from.Value.ToUnixTimeMilliseconds()}&end={to.Value.ToUnixTimeMilliseconds()}&limit={count}");

            }
            else
            {
                apiResponse = await httpClient.GetAsync($"candles/trade:{timeframe}:{pair}/hist?limit={count}");
            }
            using (apiResponse)
            {
                apiResponse.EnsureSuccessStatusCode();
                string json = await apiResponse.Content.ReadAsStringAsync();

                List<JsonElement> raw = JsonSerializer.Deserialize<List<JsonElement>>(json);

                var result = raw.Select(entry => new Candle
                {
                    OpenPrice = entry[1].GetDecimal(),
                    ClosePrice = entry[2].GetDecimal(),
                    HighPrice = entry[3].GetDecimal(),
                    LowPrice = entry[4].GetDecimal(),
                    TotalVolume = entry[5].GetDecimal(),
                    TotalPrice = entry[5].GetDecimal()* entry[2].GetDecimal(),
                    Pair = pair,
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(entry[0].GetInt64())
                });

                return result;
            }
        }
        public async Task<Ticker> GetNewTickersAsync(string symbol)
        {
            HttpResponseMessage apiResponse;

            apiResponse = await httpClient.GetAsync($"ticker/{symbol}");

            using (apiResponse)
            {
                apiResponse.EnsureSuccessStatusCode();
                string json = await apiResponse.Content.ReadAsStringAsync();

                List<JsonElement> raw = JsonSerializer.Deserialize<List<JsonElement>>(json);

                var ticker = new Ticker
                {
                    Bid = (float)raw[0].GetDecimal(),
                    BidSize = (float)raw[1].GetDecimal(),
                    Ask = (float)raw[2].GetDecimal(),
                    AskSize = (float)raw[3].GetDecimal(),
                    DailyChange = (float)raw[4].GetDecimal(),
                    DailyChangeRelative = (float)raw[5].GetDecimal(),
                    LastPrice = (float)raw[6].GetDecimal(),
                    Volume = (float)raw[7].GetDecimal(),
                    High = (float)raw[8].GetDecimal(),
                    Low = (float)raw[9].GetDecimal()
                };
                return ticker;
            }
        }
    }
}
