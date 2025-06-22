
using System.ComponentModel;

namespace HyperQuant.UI.ViewModels
{
    public class CandleViewModel : INotifyPropertyChanged
    {
        private decimal _openPrice;
        private decimal _highPrice;
        private decimal _lowPrice;
        private decimal _closePrice;
        private decimal _totalVolume;
        private decimal _totalPrice;

        public DateTime OpenTime { get; set; }
        public string Pair { get; set; } = string.Empty;

        public decimal OpenPrice
        {
            get => _openPrice;
            set
            {
                _openPrice = value;
                OnPropertyChanged(nameof(OpenPrice));
            }
        }

        public decimal HighPrice
        {
            get => _highPrice;
            set
            {
                _highPrice = value;
                OnPropertyChanged(nameof(HighPrice));
            }
        }

        public decimal LowPrice
        {
            get => _lowPrice;
            set
            {
                _lowPrice = value;
                OnPropertyChanged(nameof(LowPrice));
            }
        }

        public decimal ClosePrice
        {
            get => _closePrice;
            set
            {
                _closePrice = value;
                OnPropertyChanged(nameof(ClosePrice));
            }
        }

        public decimal TotalVolume
        {
            get => _totalVolume;
            set
            {
                _totalVolume = value;
                OnPropertyChanged(nameof(TotalVolume));
            }
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set
            {
                _totalPrice = value;
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
