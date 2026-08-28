using Asher.Services.Hosting;

namespace Asher.App.Hosting
{
    internal static class PrismApplicationServiceRegistration
    {
        public static ApplicationServices RegisterApplicationServices(IContainerRegistry containerRegistry)
        {
            var services = ApplicationServices.Create();
            ApplicationServiceRegistration.RegisterInstances(
                services,
                (serviceType, instance) => containerRegistry.RegisterInstance(serviceType, instance));

            return services;
        }
    }
}
