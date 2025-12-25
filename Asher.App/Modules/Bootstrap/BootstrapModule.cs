namespace Asher.App.Modules.Bootstrap
{
    public class BootstrapModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<RuntimeBootstrapper>();
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var bootstrapper = containerProvider.Resolve<RuntimeBootstrapper>();
            bootstrapper.Initialize();
        }
    }
}
