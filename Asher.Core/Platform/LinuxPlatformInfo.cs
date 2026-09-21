using System;
using System.IO;

namespace Asher.Core.Platform
{
    /// <summary>
    /// Linux keeps the native DustAET launcher untouched: Asher attaches through
    /// libasher_bootstrap.so (LD_PRELOAD) into the Mono runtime embedded in DustAET.
    /// There is no executable swap and no user-runnable recovery script.
    /// </summary>
    public sealed class LinuxPlatformInfo : IPlatformInfo
    {
        public PlatformKind Kind => PlatformKind.Linux;

        public string GameExecutableName => "DustAET";

        public string RealGameExecutableName => string.Empty;

        public string LauncherExecutableName => string.Empty;

        public string BootstrapLibraryName => "libasher_bootstrap.so";

        public bool UsesLauncherSwap => false;

        public bool SupportsRecoveryHelper => false;

        public string DefaultGameFolderName => "Dust An Elysian Tail";

        public string UserSettingsDirectory
        {
            get
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                string baseDirectory;

                if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                {
                    baseDirectory = xdgConfigHome;
                }
                else
                {
                    var home = Environment.GetEnvironmentVariable("HOME");
                    baseDirectory = !string.IsNullOrWhiteSpace(home)
                        ? Path.Combine(home, ".config")
                        : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }

                return Path.Combine(baseDirectory, "Asher");
            }
        }

        public bool WritesPortableSettings => false;
    }
}
