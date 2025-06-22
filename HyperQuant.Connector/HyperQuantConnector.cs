using ConnectorTest;
using HyperQuant.Connector.models;

namespace HyperQuant.Connector
{
    public class HyperQuantConnector : ITestConnector
    {
        private readonly HyperQuantREST hyperQuantREST;
        private readonly HyperQuantWS hyperQuantWS;
        private bool disposed = false;

        public HyperQuantConnector()
        {
            hyperQuantREST = new HyperQuantREST();
            hyperQuantWS = new HyperQuantWS();

            hyperQuantWS.NewBuyTrade += OnNewBuyTrade;
            hyperQuantWS.NewSellTrade += OnNewSellTrade;
            hyperQuantWS.CandleSeriesProcessing += OnCandleSeriesProcessing;
        }

        #region Rest Methods

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));

            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Количество должно быть больше 0.");

            return await hyperQuantREST.GetNewTradesAsync(pair, maxCount);
        }

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));

            if (periodInSec <= 0)
                throw new ArgumentOutOfRangeException(nameof(periodInSec), "Период должен быть больше 0.");

            return await hyperQuantREST.GetCandleSeriesAsync(pair, periodInSec, from, to, count);
        }
        public async Task <Ticker> GetNewTickersAsync(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Введите валюту",nameof(symbol));
            return await hyperQuantREST.GetNewTickersAsync(symbol);
        }

        #endregion

        #region WebSocket Events

        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;

        private void OnNewBuyTrade(Trade trade)
        {
            NewBuyTrade?.Invoke(trade);
        }

        private void OnNewSellTrade(Trade trade)
        {
            NewSellTrade?.Invoke(trade);
        }

        private void OnCandleSeriesProcessing(Candle candle)
        {
            CandleSeriesProcessing?.Invoke(candle);
        }

        #endregion

        #region WebSocket Methods

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));

            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Количество должно быть больше 0.");

            hyperQuantWS.SubscribeTrades(pair, maxCount);
        }

        public void UnsubscribeTrades(string pair)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));

            hyperQuantWS.UnsubscribeTrades(pair);
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));

            if (periodInSec <= 0)
                throw new ArgumentOutOfRangeException(nameof(periodInSec), "Период должен быть больше 0.");

            hyperQuantWS.SubscribeCandles(pair, from, to, count);
        }

        public void UnsubscribeCandles(string pair)
        {
            if (string.IsNullOrEmpty(pair))
                throw new ArgumentException("Пара не может быть пустой.", nameof(pair));

            hyperQuantWS.UnsubscribeCandles(pair);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                
                if (hyperQuantWS != null)
                {
                    hyperQuantWS.NewBuyTrade -= OnNewBuyTrade;
                    hyperQuantWS.NewSellTrade -= OnNewSellTrade;
                    hyperQuantWS.CandleSeriesProcessing -= OnCandleSeriesProcessing;
                    hyperQuantWS.Dispose();
                }

                disposed = true;
            }
        }

        #endregion
    }

}
