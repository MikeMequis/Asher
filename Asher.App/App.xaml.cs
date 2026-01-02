using Asher.Services.Implementations;
using Asher.Services.Interfaces;
using Asher.UserInterface;
using Asher.UserInterface.Views;
using System.IO;
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
            containerRegistry.RegisterSingleton<IDllInjectorService, DllInjectorService>();
            containerRegistry.RegisterSingleton<IGameLauncher, GameLauncher>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var gameFolderService = Container.Resolve<IGameFolderService>();
            var gameFolder = gameFolderService.DetectGameFolder();

            if (gameFolder.IsValid)
            {
                var launcher = Container.Resolve<IGameLauncher>();
                launcher.Launch(Path.Combine(gameFolder.Path, "DustAET.exe"));
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ViewsModule>();
        }
    }
}
