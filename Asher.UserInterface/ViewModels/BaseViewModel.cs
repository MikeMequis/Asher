using Asher.Localization;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Asher.UserInterface.ViewModels
{
    public abstract class BaseViewModel : BindableBase, INavigationAware
    {
        protected BaseViewModel()
        {
            SubscribeToLanguageChanges();
        }

        private void SubscribeToLanguageChanges()
        {
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, CultureInfo newCulture)
        {
            RaisePropertyChanged("Item[]");
        }

        public abstract Task InitAsync();
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }
        public virtual void OnNavigatedTo(NavigationContext navigationContext) { }

        [IndexerName("LanguageKey")]
        public string this[string key] => LocalizationManager.Instance[key];
    }
}
