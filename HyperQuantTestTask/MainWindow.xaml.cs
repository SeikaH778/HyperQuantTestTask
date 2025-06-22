using HyperQuant.Connector;
using HyperQuant.Connector.models;
using System.Collections.ObjectModel;
using System.Windows;

namespace HyperQuantTestTask
{
    public partial class MainWindow : Window
    {
        private readonly HyperQuantREST hyperQuantRESTClient = new HyperQuantREST();
        private readonly PorfolioCalculate CalculatePortfolio = new PorfolioCalculate();
        private readonly ObservableCollection<PortfolioConversion> portfolioData = new ObservableCollection<PortfolioConversion>();

        private WebSocketWindow? webSocketWindow;
        private readonly ObservableCollection<string> currencies;

        public MainWindow()
        {
            InitializeComponent();
            CurrenciesComboBox.ItemsSource = currencies;
            PortfolioDataGrid.ItemsSource = portfolioData;

            _ = LoadPortfolioDataAsync();
        }
        private async Task LoadPortfolioDataAsync()
        {
            try
            {
                var portfolioResult = await CalculatePortfolio.CalculatePortfolioAsync();

                Dispatcher.Invoke(() =>
                {
                    portfolioData.Clear();
                    foreach (var item in portfolioResult)
                    {
                        portfolioData.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
               
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при загрузке портфеля: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            { 
            }
        }
        private async void GetTradesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetTradesButton.IsEnabled = false;
                GetTradesButton.Content = "Загрузка...";

                var trades = await hyperQuantRESTClient.GetNewTradesAsync(SymbolTextBox.Text.Trim(), 10);
                TradesListView.Items.Clear();
                foreach (var trade in trades)
                {
                    TradesListView.Items.Add($"{trade.Pair} | {trade.Id} | {trade.Time} | {trade.Amount}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сделок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GetTradesButton.IsEnabled = true;
                GetTradesButton.Content = "Получить сделки";
            }
        }

        private async void GetCandleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetCandleButton.IsEnabled = false;
                GetCandleButton.Content = "Загрузка...";

                var candles = await hyperQuantRESTClient.GetCandleSeriesAsync(SymbolTextBox.Text.Trim(), 60, null, null, 10);
                CandleListView.Items.Clear();
                foreach (var candle in candles)
                {
                    CandleListView.Items.Add($"{candle.Pair} | {candle.OpenTime} | {candle.OpenPrice} | {candle.ClosePrice} | {candle.HighPrice} | {candle.LowPrice} | " +
                        $"{candle.TotalVolume} | {candle.TotalPrice}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки свечей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GetCandleButton.IsEnabled = true;
                GetCandleButton.Content = "Получить свечи";
            }
        }

        private void OpenWebSocketButton_Click(object sender, RoutedEventArgs e)
        {
            if (webSocketWindow == null || !webSocketWindow.IsLoaded)
            {
                webSocketWindow = new WebSocketWindow();
                webSocketWindow.Owner = this;
                webSocketWindow.Show();
            }
            else
            {
                webSocketWindow.Activate();
                webSocketWindow.WindowState = WindowState.Normal;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            webSocketWindow?.Close();
            base.OnClosed(e);
        }

        private async void GetTickerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetTickerButton.IsEnabled = false;
                GetTickerButton.Content = "Загрузка...";
                var ticker = await hyperQuantRESTClient.GetNewTickersAsync(SymbolTextBox.Text.Trim());
                TickersListView.Items.Clear();
                
                TickersListView.Items.Add($"{ticker.Bid} | {ticker.BidSize} | {ticker.Ask} | {ticker.AskSize} | {ticker.DailyChange} | {ticker.DailyChangeRelative}" +
                        $" | {ticker.High} | {ticker.Low} | {ticker.LastPrice} | {ticker.Volume}");
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тикера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GetTickerButton.IsEnabled = true;
                GetTickerButton.Content = "Получить тикер";
            }

        }

       
    }
}