using HyperQuant.Connector;
using HyperQuant.Connector.models;
using System.Text.Json;

public class PorfolioCalculate
{
    private readonly HyperQuantREST hyperQuantREST;

    public PorfolioCalculate()
    {
        hyperQuantREST = new HyperQuantREST();
    }

    public async Task<List<PortfolioConversion>> CalculatePortfolioAsync()
    {
        // Загрузка баланса из файла
        var balances = LoadBalancesFromFile(@"balance.json");
        if (balances.Count == 0)
        {
            return new List<PortfolioConversion>();
        }

        var symbols = balances.Select(b => b.Symbol).Distinct().ToList();
        var targetCurrencies = new[] { "USD", "BTC", "XRP", "XMR", "DASH" };
        var tickers = new Dictionary<string, Ticker>();

        foreach (var from in symbols)
        {
            foreach (var to in targetCurrencies)
            {
                if (from == to) continue;

                string direct = $"t{from}{to}";
                string reverse = $"t{to}{from}";

                try
                {
                    var ticker = await hyperQuantREST.GetNewTickersAsync(direct);
                    if (ticker != null && ticker.LastPrice > 0)
                    {
                        tickers[direct] = ticker;
                    }
                    else
                    {
                        throw new Exception($"Не удалось получить тикер для {direct}.");
                    }
                }
                catch (Exception ex)
                {
                    
                    try
                    {
                        
                        var ticker = await hyperQuantREST.GetNewTickersAsync(reverse);
                        if (ticker != null && ticker.LastPrice > 0)
                        {
                            tickers[reverse] = ticker;
                            
                        }
                        else
                        {
                            throw new Exception($"Не удалось получить тикер для {reverse}.");
                        }
                    }
                    catch (Exception ex2)
                    {
                       throw new Exception($"Ошибка при получении тикера для {from} в {to}: {ex2.Message}", ex2);
                    }
                }
            }
        }

        var result = new List<PortfolioConversion>();
        foreach (var target in targetCurrencies)
        {
            float total = 0f;
            foreach (var asset in balances)
            {
                var from = asset.Symbol;
                float amount = asset.Amount;

                if (from == target)
                {
                    total += amount;
                    continue;
                }

                string direct = $"t{from}{target}";
                string reverse = $"t{target}{from}";

                if (tickers.TryGetValue(direct, out var ticker))
                {
                    float converted = amount * ticker.LastPrice;
                    total += converted;
                }
                else if (tickers.TryGetValue(reverse, out ticker) && ticker.LastPrice > 0)
                {
                    float converted = amount / ticker.LastPrice;
                    total += converted;
                }
                else
                {
                    throw new Exception($"Не удалось найти тикер для конвертации {from} в {target}.");
                }
            }

            result.Add(new PortfolioConversion
            {
                Currency = target,
                TotalValue = (float)Math.Round(total, 2)
            });
        }

        return result;
    }

    private List<BalanceEntry> LoadBalancesFromFile(string path)
    {
        try
        {

            string json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var data = JsonSerializer.Deserialize<BalanceData>(json, options);

            if (data?.Currencies != null)
            {
               
                return data.Currencies;
            }
            else
            {
                
                return new List<BalanceEntry>();
            }
        }
        catch (Exception ex)
        {
            
            throw ex;
        }
    }
}