using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Asher.Localization
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private ResourceManager? _resourceManager;
        private CultureInfo _uiLanguage = new("en-US");

        public static event EventHandler<CultureInfo>? LanguageChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CultureInfo UILanguage
        {
            get => _uiLanguage;
            set
            {
                var culture = value ?? new CultureInfo("en-US");
                if (_uiLanguage.Name == culture.Name)
                    return;

                _uiLanguage = culture;
                OnPropertyChanged(nameof(UILanguage));
                OnPropertyChanged("Item[]");
                LanguageChanged?.Invoke(this, culture);
            }
        }

        public static void Initialize(string? language = null)
        {
            Instance.ApplyCulture(language);
        }

        public void ApplyCulture(string? language)
        {
            _resourceManager ??= new ResourceManager(
                "Asher.Localization.Resources.Strings",
                typeof(LocalizationManager).Assembly);

            UILanguage = ResolveCulture(language);
        }

        public static CultureInfo[] GetSupportedCultures() =>
        [
            new CultureInfo("en-US"),
            new CultureInfo("pt-BR"),
            new CultureInfo("es-ES")
        ];

        public static string GetCultureDisplayName(CultureInfo culture) => culture.Name switch
        {
            "pt-BR" => "Portuguese (Brazil)",
            "es-ES" => "Spanish",
            _ => "English"
        };

        public static string GetCultureNameFromDisplay(string displayName) => displayName switch
        {
            "Portuguese (Brazil)" => "pt-BR",
            "Spanish" => "es-ES",
            _ => "en-US"
        };

        public string this[string key]
        {
            get
            {
                try
                {
                    if (_resourceManager == null)
                        return key;

                    return _resourceManager.GetString(key, _uiLanguage)
                        ?? _resourceManager.GetString(key, new CultureInfo("en-US"))
                        ?? key;
                }
                catch
                {
                    return key;
                }
            }
        }

        private static CultureInfo ResolveCulture(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return new CultureInfo("en-US");

            try
            {
                return CultureInfo.GetCultureInfo(language);
            }
            catch
            {
                return new CultureInfo("en-US");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
