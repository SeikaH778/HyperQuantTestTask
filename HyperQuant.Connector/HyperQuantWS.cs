using HyperQuant.Connector.models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace HyperQuant.Connector
{
    public class HyperQuantWS : IDisposable
    {
        private ClientWebSocket? bitFinexWS;
        static CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;
        private readonly Uri bitFinexWSUri = new("wss://api-pub.bitfinex.com/ws/2");
        private int tradeChannelId;
        private int candleChannelId;
        private readonly int bufferSize = 4096;
        private bool disposed = false;
        private static long examplePeriodInSec = 60; // пример периода в секундах
        private string? currentPair;
        private string timeFrame = TimeFrames.GetTimeframe(examplePeriodInSec);
        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;
        public event Action<Trade>? TradeReceived;
        public event Action<string>? ErrorOccurred;
        public event Action? Connected;

        public async Task ConnectAsync(string symbol, CancellationToken cancellationToken)
        {
            
            if (bitFinexWS?.State == WebSocketState.Open)
                return; 
            bitFinexWS?.Dispose();
            bitFinexWS = new ClientWebSocket();

            try
            {
                await bitFinexWS.ConnectAsync(bitFinexWSUri, cancellationToken);

                var subscribeTrade = new
                {
                    @event = "subscribe",
                    channel = "trades",
                    symbol = symbol
                };
                var subscribeCandles = new
                {
                    @event = "subscribe",
                    channel = "candles",
                    key = $"trade:{timeFrame}:{symbol}" 
                };
                string tradeJson = JsonSerializer.Serialize(subscribeTrade);
                byte[] tradeBytes = Encoding.UTF8.GetBytes(tradeJson);
                await bitFinexWS.SendAsync(
                    new ArraySegment<byte>(tradeBytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
                Connected?.Invoke();
                string candleJson = JsonSerializer.Serialize(subscribeCandles);
                byte[] candleBytes = Encoding.UTF8.GetBytes(candleJson);
                await bitFinexWS.SendAsync(
                    new ArraySegment<byte>(candleBytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Ошибка подключения: {ex.Message}");
                throw;
            }
        }

        public async Task ReceiveLoopAsync(string pair, CancellationToken cancellationToken)
        {
            if (bitFinexWS?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket не подключен. Вызовите ConnectAsync сначала.");
            }

            var messageBuffer = new List<byte>();

            while (bitFinexWS.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var buffer = new byte[bufferSize];
                WebSocketReceiveResult result;

                try
                {
                    result = await bitFinexWS.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break; 
                }
                catch (WebSocketException ex)
                {
                    ErrorOccurred?.Invoke($"WebSocket ошибка: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"Неожиданная ошибка: {ex.Message}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await bitFinexWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие подключения", cancellationToken);
                    break;
                }

               
                messageBuffer.AddRange(buffer.Take(result.Count));

                
                if (result.EndOfMessage)
                {
                    string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    messageBuffer.Clear();

                    try
                    {
                        if (message.StartsWith("{"))
                            await HandleEventMessage(message, pair);
                        else if (message.StartsWith("["))
                            HandleDataMessage(message, pair);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke($"Ошибка обработки сообщения: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleEventMessage(string json, string pair)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("event", out var eventProp))
                    return;

                string eventType = eventProp.GetString() ?? string.Empty;
                switch (eventType)
                {
                    case "subscribed":
                        if (root.TryGetProperty("channel", out var channelProp))
                        {
                            string channel = channelProp.GetString() ?? string.Empty;
                            int chanId = root.GetProperty("chanId").GetInt32();
                            if (channel == "trades")
                                tradeChannelId = chanId;
                            else if (channel == "candles")
                                candleChannelId = chanId;
                        }
                        break;

                    case "error":
                        string errorMsg = root.TryGetProperty("msg", out var msg) ?
                            msg.GetString() ?? "Неизвестная ошибка" : "Неизвестная ошибка";
                        ErrorOccurred?.Invoke($"Ошибка подписки: {errorMsg}");
                        break;

                    case "info":
                        if (root.TryGetProperty("code", out var code))
                        {
                            int infoCode = code.GetInt32();

                            if (infoCode == 20051 || infoCode == 20060) // Коды Bitfinex для переподключения
                            {
                                ErrorOccurred?.Invoke($"Требуется переподключение (код: {infoCode})");
                                throw new OperationCanceledException("Bitfinex запросил переподключение.");
                            }
                        }
                        break;
                }
                
            }
            catch (JsonException ex)
            {
                ErrorOccurred?.Invoke($"Ошибка парсинга JSON события: {ex.Message}");
            }
        }

        private void HandleDataMessage(string json, string pair)
        {
            try
            {
                var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
                if (elements == null || elements.Count < 2)
                    return;
                int channelId = elements[0].GetInt32();
                 
                    if (channelId != tradeChannelId && channelId != candleChannelId)
                        return;
                    if (channelId == candleChannelId)
                    {
                        if (elements[1].ValueKind == JsonValueKind.String && elements[1].GetString() == "hb")
                            return;
                        if (elements[1].ValueKind == JsonValueKind.Array && elements[1][0].ValueKind == JsonValueKind.Array)
                        {
                            // Массив свечей
                            foreach (var item in elements[1].EnumerateArray())
                            {
                                var candle = new Candle
                                {
                                    Pair = pair,
                                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()).DateTime,
                                    OpenPrice = item[1].GetDecimal(),
                                    ClosePrice = item[2].GetDecimal(),
                                    HighPrice = item[3].GetDecimal(),
                                    LowPrice = item[4].GetDecimal(),
                                    TotalVolume = item[5].GetDecimal()
                                };
                                CandleSeriesProcessing?.Invoke(candle);
                            }
                        }
                        else if (elements[1].ValueKind == JsonValueKind.Array)
                        {
                            // Одиночная свеча
                            var candleData = elements[1];
                            var candle = new Candle
                            {
                                Pair = pair,
                                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleData[0].GetInt64()).DateTime,
                                OpenPrice = candleData[1].GetDecimal(),
                                ClosePrice = candleData[2].GetDecimal(),
                                HighPrice = candleData[3].GetDecimal(),
                                LowPrice = candleData[4].GetDecimal(),
                                TotalVolume = candleData[5].GetDecimal()
                            };
                            CandleSeriesProcessing?.Invoke(candle);
                        }
                    }
                    if (elements[1].ValueKind == JsonValueKind.String && elements[1].GetString() == "tu")
                    {
                        if (elements.Count < 3 || elements[2].ValueKind != JsonValueKind.Array)
                            return;

                        var tradeData = elements[2];
                        if (tradeData.GetArrayLength() < 4)
                            return;

                        var trade = new Trade
                        {
                            Id = tradeData[0].ToString() ?? string.Empty,
                            Time = DateTimeOffset.FromUnixTimeMilliseconds(tradeData[1].GetInt64()).DateTime,
                            Amount = tradeData[2].GetDecimal(),
                            Price = tradeData[3].GetDecimal(),
                            Pair = pair
                        };
                        TradeReceived?.Invoke(trade);
                        if (trade.Amount > 0)
                        {
                            NewBuyTrade?.Invoke(trade);
                        }
                        else if (trade.Amount < 0)
                        {
                            NewSellTrade?.Invoke(trade);
                        }
                    }
                
            }
            catch (JsonException ex)
            {
                ErrorOccurred?.Invoke($"Ошибка парсинга JSON торгов: {ex.Message}");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Ошибка обработки торгового сообщения: {ex.Message}");
            }
        }
      

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (bitFinexWS?.State == WebSocketState.Open)
            {
                try
                {
                    await bitFinexWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Отключение по запросу", cancellationToken);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"Ошибка при отключении: {ex.Message}");
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                bitFinexWS?.Dispose();
                disposed = true;
            }
        }
        public async Task SubscribeTrades(string pair, int maxCount = 100) 
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));
            if (string.IsNullOrEmpty(timeFrame))
                throw new ArgumentOutOfRangeException(nameof(examplePeriodInSec), "Не верный период");
            if (currentPair == pair && bitFinexWS?.State == WebSocketState.Open)
                return; // если уже подписаны на эту пару
             await UnsubscribeTrades(currentPair, token); 
            {
                currentPair = pair;
                _ = Task.Run(async () =>
                {
                    await ConnectAsync(pair, token);
                    await ReceiveLoopAsync(pair, token);
                });
            }
        }
        public async Task UnsubscribeTrades(string pair, CancellationToken cancellationToken = default )
        {
            var unsubscribeTrade = new
            {
                @event = "unsubscribe",
                chanId = tradeChannelId
            };
            string unsubscribeTradejson = JsonSerializer.Serialize(unsubscribeTrade);
            byte[] unsubscribeTradeBytes = Encoding.UTF8.GetBytes(unsubscribeTradejson);
            await bitFinexWS.SendAsync(
                   new ArraySegment<byte>(unsubscribeTradeBytes),
                   WebSocketMessageType.Text,
                   true,
                   cancellationToken);
        }
        public void SubscribeCandles(string pair, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой", nameof(pair));
            if (string.IsNullOrEmpty(timeFrame))
                throw new ArgumentException("Неподдерживаемый период", nameof(timeFrame));
            _ = Task.Run(async () =>
            {
                await ConnectAsync(pair, token);
                await ReceiveLoopAsync(pair, token);
            });
        }

        public void UnsubscribeCandles(string pair)
        {
            
            _ = DisconnectAsync();
        }
    }
}

