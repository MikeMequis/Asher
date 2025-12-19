using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Asher.Localization
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager _instance;
        public static LocalizationManager Instance => _instance ??= new();

        private ResourceManager _resourceManager;
        private CultureInfo _uiLanguage = new("pt-BR");

        public ResourceManager ResourceManager
        {
            get => _resourceManager;
            set => _resourceManager = value;
        }

        public CultureInfo UILanguage
        {
            get => _uiLanguage;
            set
            {
                if (_uiLanguage?.Name != value?.Name)
                {
                    _uiLanguage = value;

                    OnPropertyChanged(nameof(UILanguage));
                    OnPropertyChanged("Item[]");

                    LanguageChanged?.Invoke(this, value);
                }
            }
        }

        public static event EventHandler<CultureInfo> LanguageChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    return _resourceManager?.GetString(key, _uiLanguage) ?? string.Empty;
                }
                catch
                {
                    return "####";
                }
            }
        }

        //public static event EventHandler<CultureInfo>? CultureChanged;
        //public event PropertyChangedEventHandler? PropertyChanged;
        //private static readonly ResourceManager _resourceManager;
        //private static CultureInfo _currentCulture;
        //private static readonly LocalizationManager _instance = new();
        //public static LocalizationManager Instance => _instance;
        //public static CultureInfo CurrentCulture => _currentCulture;
        //public CultureInfo CurrentCultureProperty => _currentCulture;

        //static LocalizationManager()
        //{
        //    _resourceManager = new ResourceManager("Asher.Localization.Resources.Strings", typeof(LocalizationManager).Assembly);
        //    _currentCulture = CultureInfo.CurrentCulture;
        //}
        
        //public static string GetString(string key) => _resourceManager.GetString(key, _currentCulture) ?? key;

        //public static string GetString(string key, params object[] args)
        //{
        //    var format = GetString(key);
        //    return string.Format(format, args);
        //}

        //public static string GetString(string key, CultureInfo? culture)
        //{
        //    if (string.IsNullOrEmpty(key))
        //        return "";
        //    return _resourceManager.GetString(key, culture ?? _currentCulture) ?? $"##{key}##";
        //}

        //public static void ChangeCulture(CultureInfo culture)
        //{
        //    _currentCulture = culture ?? throw new ArgumentNullException(nameof(culture));
        //    CultureChanged?.Invoke(null, culture);
        //    _instance.PropertyChanged?.Invoke(_instance, new PropertyChangedEventArgs(nameof(CurrentCultureProperty)));
        //}

        //public static CultureInfo[] GetAvailableCultures()
        //{
        //    return
        //    [
        //        new CultureInfo("en-US"), // English
        //        new CultureInfo("pt-BR"), // Portuguese (Brazil)
        //    ];
        //}

        //public static void ToggleLanguage()
        //{
        //    var availableCultures = GetAvailableCultures();
        //    var currentCultureName = _currentCulture.Name;
        //    var nextCulture = availableCultures.First(c => c.Name != currentCultureName);
        //    ChangeCulture(nextCulture);
        //}
    }
} 