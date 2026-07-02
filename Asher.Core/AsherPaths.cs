using System.IO;

namespace Asher.Core
{
    public static class AsherPaths
    {
        public const string RuntimeFolderName = "Asher";
        public const string ManagerFolderName = "Asher.App";
        public const string BackupFolderName = "Asher.Backup";
        public const string DefaultModsFolderName = "DefaultMods";
        public const string SettingsFileName = "settings.json";

        public const string GameExecutableName = "DustAET.exe";
        public const string RealGameExecutableName = "DustAET.real.exe";
        public const string LauncherExecutableName = "Asher.Launcher.exe";

        public static string GetAppBaseDirectory() =>
            AppDomain.CurrentDomain.BaseDirectory;

        public static string GetLocalSettingsPath() =>
            Path.Combine(GetAppBaseDirectory(), SettingsFileName);

        public static string GetManagerFolderPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, ManagerFolderName);

        public static string? TryGetGameFolderFromManagerLocation()
        {
            var baseDir = GetAppBaseDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Path.GetFileName(baseDir).Equals(ManagerFolderName, StringComparison.OrdinalIgnoreCase))
                return null;

            var parent = Directory.GetParent(baseDir)?.FullName;
            return parent != null && IsValidGameFolder(parent) ? parent : null;
        }

        public static bool IsValidGameFolder(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
                return false;

            return File.Exists(Path.Combine(gameFolderPath, GameExecutableName))
                || (File.Exists(Path.Combine(gameFolderPath, RealGameExecutableName))
                    && Directory.Exists(Path.Combine(gameFolderPath, RuntimeFolderName)));
        }

        public static bool IsAsherInstalledIn(string gameFolderPath)
        {
            if (!IsValidGameFolder(gameFolderPath))
                return false;

            return File.Exists(Path.Combine(gameFolderPath, RealGameExecutableName))
                && Directory.Exists(Path.Combine(gameFolderPath, RuntimeFolderName));
        }
    }
}
