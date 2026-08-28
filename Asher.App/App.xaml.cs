using Asher.App.Hosting;
using Asher.Core;
using Asher.Localization;
using Asher.Services.Implementations;
using Asher.Services.Interfaces;
using Asher.UserInterface;
using Asher.UserInterface.Services;
using Asher.UserInterface.Views;
using System.Windows;

namespace Asher.App
{
    public partial class App : PrismApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            LocalizationManager.Initialize(settings.Language);
            new ThemeService().Apply(
                string.IsNullOrWhiteSpace(settings.Theme) ? "Light" : settings.Theme);

            base.OnStartup(e);
        }

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var settings = Container.Resolve<ISettingsService>().Load();
            Container.Resolve<IThemeService>().Apply(
                string.IsNullOrWhiteSpace(settings.Theme) ? "Light" : settings.Theme);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            PrismApplicationServiceRegistration.RegisterApplicationServices(containerRegistry);

            containerRegistry.Register<MainWindow>();
            containerRegistry.RegisterSingleton<INavigationItemsManager, NavigationItemsManager>();
            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ViewsModule>();
        }
    }
}
