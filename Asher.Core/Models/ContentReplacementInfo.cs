namespace Asher.Core.Models
{
    public class ContentReplacementInfo : BindableBase
    {
        public string Target { get; set; } = string.Empty;
        public string FromFile { get; set; } = string.Empty;

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }
}
