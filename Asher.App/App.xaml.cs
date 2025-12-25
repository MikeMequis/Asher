using Asher.App.Modules.Bootstrap;
using Asher.Services.Implementations;
using Asher.Services.Interfaces;
using Asher.UserInterface;
using Asher.UserInterface.Views;
using System.Windows;

namespace Asher.App
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<MainWindow>();
            containerRegistry.RegisterSingleton<IGameFolderService, GameFolderService>();
            containerRegistry.RegisterSingleton<IHarmonyService, HarmonyService>();
            containerRegistry.RegisterSingleton<IPatchManagerService, PatchManagerService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ViewsModule>();
            moduleCatalog.AddModule<BootstrapModule>();
        }
    }
}