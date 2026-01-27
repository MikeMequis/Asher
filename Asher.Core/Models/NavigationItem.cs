using MaterialDesignThemes.Wpf;

namespace Asher.Core.Models
{
    public class NavigationItem : BindableBase
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string NavigationPath { get; set; } = string.Empty;
        public PackIconKind Icon { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }
}