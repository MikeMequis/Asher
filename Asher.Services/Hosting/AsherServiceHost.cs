using Asher.Services.Application;

namespace Asher.Services.Hosting
{
    /// <summary>
    /// Wires application services without WPF/Prism. Used by Asher.Host and Electron.
    /// </summary>
    public sealed class AsherServiceHost
    {
        public IAsherApplication Application { get; }

        private AsherServiceHost(IAsherApplication application)
        {
            Application = application;
        }

        public static AsherServiceHost Create()
        {
            var services = ApplicationServices.Create();
            return new AsherServiceHost(new AsherApplication(services));
        }
    }
}
