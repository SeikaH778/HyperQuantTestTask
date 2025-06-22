
using System.ComponentModel;

namespace HyperQuant.UI.ViewModels
{
    public class TradeViewModel : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Pair { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
