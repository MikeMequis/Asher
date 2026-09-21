using System;
using System.IO;

namespace Asher.Core.Platform
{
    /// <summary>
    /// Windows uses the launcher/real-executable swap model:
    /// DustAET.exe is replaced by Asher.Launcher.exe and the original becomes DustAET.real.exe.
    /// </summary>
    public sealed class WindowsPlatformInfo : IPlatformInfo
    {
        public PlatformKind Kind => PlatformKind.Windows;

        public string GameExecutableName => "DustAET.exe";

        public string RealGameExecutableName => "DustAET.real.exe";

        public string LauncherExecutableName => "Asher.Launcher.exe";

        public string BootstrapLibraryName => string.Empty;

        public bool UsesLauncherSwap => true;

        public bool SupportsRecoveryHelper => true;

        public string DefaultGameFolderName => "Dust An Elysian Tail";

        public string UserSettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Asher");

        public bool WritesPortableSettings => true;
    }
}
