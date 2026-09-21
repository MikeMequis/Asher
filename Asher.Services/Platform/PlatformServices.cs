using Asher.Core.Platform;
using Asher.Services.Interfaces;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Composition root for the platform-dependent operations. One entry point so
    /// <see cref="Hosting.ApplicationServices"/> does not branch on the OS itself.
    /// </summary>
    public sealed class PlatformServices
    {
        public IPlatformInfo Info { get; }
        public IGameFolderDiscovery GameFolderDiscovery { get; }
        public IGameProcessLauncher ProcessLauncher { get; }
        public IGameExecutableLayout ExecutableLayout { get; }
        public IRuntimeDeployment RuntimeDeployment { get; }

        private PlatformServices(
            IPlatformInfo info,
            IGameFolderDiscovery gameFolderDiscovery,
            IGameProcessLauncher processLauncher,
            IGameExecutableLayout executableLayout,
            IRuntimeDeployment runtimeDeployment)
        {
            Info = info;
            GameFolderDiscovery = gameFolderDiscovery;
            ProcessLauncher = processLauncher;
            ExecutableLayout = executableLayout;
            RuntimeDeployment = runtimeDeployment;
        }

        public static PlatformServices Create()
        {
            var info = PlatformInfo.Current;
            var processStarter = new SystemProcessStarter();

            if (info.Kind == PlatformKind.Linux)
            {
                var deployment = new LinuxRuntimeDeployment(info);

                return new PlatformServices(
                    info,
                    new LinuxGameFolderDiscovery(info),
                    new LinuxGameProcessLauncher(info, deployment, processStarter, new SystemEnvironmentProvider()),
                    new LinuxGameExecutableLayout(info),
                    deployment);
            }

            return new PlatformServices(
                info,
                new WindowsGameFolderDiscovery(),
                new WindowsGameProcessLauncher(processStarter),
                new WindowsGameExecutableLayout(),
                new WindowsRuntimeDeployment());
        }
    }
}
