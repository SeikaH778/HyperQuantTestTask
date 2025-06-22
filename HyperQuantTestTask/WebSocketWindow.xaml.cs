using HyperQuant.Connector;
using HyperQuant.Connector.models;
using HyperQuant.UI.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace HyperQuantTestTask
{
    public partial class WebSocketWindow : Window
    {
        private readonly HyperQuantWS hyperQuantWSClient;
        private readonly ObservableCollection<TradeViewModel> trades;
        private readonly ObservableCollection<CandleViewModel> candles;
        private readonly DispatcherTimer timer;

        private int buyTradesCount = 0;
        private int sellTradesCount = 0;
        private int candlesCount = 0;
        private bool isConnected = false;

        public WebSocketWindow()
        {
            InitializeComponent();

            hyperQuantWSClient = new HyperQuantWS();
            trades = new ObservableCollection<TradeViewModel>();
            candles = new ObservableCollection<CandleViewModel>();

            TradesListView.ItemsSource = trades;
            CandlesListView.ItemsSource = candles;

            hyperQuantWSClient.NewBuyTrade += OnNewBuyTrade;
            hyperQuantWSClient.NewSellTrade += OnNewSellTrade;
            hyperQuantWSClient.CandleSeriesProcessing += OnCandleReceived;
            hyperQuantWSClient.Connected += OnConnected;
            hyperQuantWSClient.ErrorOccurred += OnErrorOccurred;

            // Таймер для обновления времени последнего обновления
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isConnected)
            {
                LastUpdateText.Text = $"Подключено: {DateTime.Now:HH:mm:ss}";
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pair = PairTextBox.Text.Trim();
                if (string.IsNullOrEmpty(pair))
                {
                    MessageBox.Show("Введите пару.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ConnectButton.IsEnabled = false;
                StatusText.Text = "Подключение...";
                ConnectionStatusText.Text = "Подключение...";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;

                
                if (SubscribeTradesCheckBox.IsChecked == true)
                {
                    hyperQuantWSClient.SubscribeTrades(pair);
                }

                if (SubscribeCandlesCheckBox.IsChecked == true)
                {
                    hyperQuantWSClient.SubscribeCandles(pair);
                }

                StatusText.Text = $"Подключено к {pair}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectButton.IsEnabled = true;
                StatusText.Text = "Ошибка подключения";
                ConnectionStatusText.Text = "Отключено";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pair = PairTextBox.Text.Trim();

                DisconnectButton.IsEnabled = false;
                StatusText.Text = "Отключение...";

                hyperQuantWSClient.UnsubscribeTrades(pair);
                hyperQuantWSClient.UnsubscribeCandles(pair);

                isConnected = false;
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                ConnectionStatusText.Text = "Отключено";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                StatusText.Text = "Отключено";
                LastUpdateText.Text = "Последнее обновление: -";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            trades.Clear();
            candles.Clear();
            buyTradesCount = 0;
            sellTradesCount = 0;
            candlesCount = 0;
            UpdateCounters();
        }

        private void OnConnected()
        {
            Dispatcher.Invoke(() =>
            {
                isConnected = true;
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                ConnectionStatusText.Text = "Подключено";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"Подключено к {PairTextBox.Text}";
            });
        }

        private void OnNewBuyTrade(Trade trade)
        {
            Dispatcher.Invoke(() =>
            {
                var tradeViewModel = new TradeViewModel
                {
                    Id = trade.Id,
                    Time = trade.Time.UtcDateTime,
                    Price = trade.Price,
                    Amount = Math.Abs(trade.Amount), 
                    Type = "BUY",
                    Pair = trade.Pair
                };

                trades.Insert(0, tradeViewModel); 
                buyTradesCount++;

                // Ограничение количества отображаемых сделок
                if (trades.Count > 100)
                {
                    trades.RemoveAt(trades.Count - 1);
                }

                UpdateCounters();
                LastUpdateText.Text = $"Последняя сделка: {DateTime.Now:HH:mm:ss}";
            });
        }

        private void OnNewSellTrade(Trade trade)
        {
            Dispatcher.Invoke(() =>
            {
                var tradeViewModel = new TradeViewModel
                {
                    Id = trade.Id,
                    Time = trade.Time.UtcDateTime,
                    Price = trade.Price,
                    Amount = Math.Abs(trade.Amount), 
                    Type = "SELL",
                    Pair = trade.Pair
                };

                trades.Insert(0, tradeViewModel); 
                sellTradesCount++;

                // Ограничение количества отображаемых сделок
                if (trades.Count > 100)
                {
                    trades.RemoveAt(trades.Count - 1);
                }

                UpdateCounters();
                LastUpdateText.Text = $"Последняя сделка: {DateTime.Now:HH:mm:ss}";
            });
        }

        private void OnCandleReceived(Candle candle)
        {
            if (candle == null)
            {
                MessageBox.Show($"Свеча Null", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                Dispatcher.Invoke(() =>
                {

                    var candleViewModel = new CandleViewModel
                    {
                        OpenTime = candle.OpenTime.DateTime,
                        OpenPrice = candle.OpenPrice,
                        HighPrice = candle.HighPrice,
                        LowPrice = candle.LowPrice,
                        ClosePrice = candle.ClosePrice,
                        TotalVolume = candle.TotalVolume,
                        TotalPrice = candle.TotalPrice,
                        Pair = candle.Pair
                    };

                    // Проверка на обновление существующей свечи
                    var existingCandle = candles.Count > 0 ? candles[0] : null;
                    if (existingCandle != null && existingCandle.OpenTime == candle.OpenTime.DateTime)
                    {
                        // Обновлениесуществующующей свечи
                        existingCandle.HighPrice = candle.HighPrice;
                        existingCandle.LowPrice = candle.LowPrice;
                        existingCandle.ClosePrice = candle.ClosePrice;
                        existingCandle.TotalVolume = candle.TotalVolume;
                        existingCandle.TotalPrice = candle.TotalPrice;
                    }
                    else
                    {
                        // Добавляе новой свечи
                        candles.Insert(0, candleViewModel);
                        candlesCount++;
                    }

                    // Ограничение количества отображаемых свечь
                    if (candles.Count > 50)
                    {
                        candles.RemoveAt(candles.Count - 1);
                    }

                    UpdateCounters();
                    LastUpdateText.Text = $"Последняя свеча: {DateTime.Now:HH:mm:ss}";
                });
            }
            catch (Exception ex) 
            {
               
            }

        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Ошибка: {error}";
                MessageBox.Show($"Ошибка: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                isConnected = false;
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                ConnectionStatusText.Text = "Ошибка";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
            });
        }

        private void UpdateCounters()
        {
            BuyTradesCountText.Text = buyTradesCount.ToString();
            SellTradesCountText.Text = sellTradesCount.ToString();
            CandlesCountText.Text = candlesCount.ToString();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                timer?.Stop();
                hyperQuantWSClient?.Dispose();
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии: {ex.Message}");
            }
            base.OnClosing(e);
        }
    }
}