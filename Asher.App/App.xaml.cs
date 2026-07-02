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
            var settings = AsherSettings.Load();
            LocalizationManager.Initialize(settings.Language);
            new ThemeService().Apply(
                string.IsNullOrWhiteSpace(settings.Theme) ? "Light" : settings.Theme);
            base.OnStartup(e);
        }

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var settings = AsherSettings.Load();
            Container.Resolve<IThemeService>().Apply(
                string.IsNullOrWhiteSpace(settings.Theme) ? "Light" : settings.Theme);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<MainWindow>();
            containerRegistry.RegisterSingleton<IGameFolderService, GameFolderService>();
            containerRegistry.RegisterSingleton<IPatchManagerService, PatchManagerService>();
            containerRegistry.RegisterSingleton<IGameInstallationService, GameInstallationService>();
            containerRegistry.RegisterSingleton<IInstallationStateService, InstallationStateService>();
            containerRegistry.RegisterSingleton<INavigationItemsManager, NavigationItemsManager>();
            containerRegistry.RegisterSingleton<IGameLaunchService, GameLaunchService>();
            containerRegistry.RegisterSingleton<IShortcutService, ShortcutService>();
            containerRegistry.RegisterSingleton<IManagerLaunchService, ManagerLaunchService>();
            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ViewsModule>();
        }
    }
}
