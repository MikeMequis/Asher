using System.IO;

namespace Asher.Core
{
    public static class AsherPaths
    {
        public const string RuntimeFolderName = "Asher";
        public const string ManagerFolderName = "Asher.App";
        public const string BackupFolderName = "Asher.Backup";
        public const string PatchesFolderName = "patches";
        public const string ContentPatchConfigFileName = "content.json";
        public const string ContentPatchAssetsFolderName = "assets";
        public const string DefaultModsFolderName = "DefaultMods";
        public const string InstallPayloadFolderName = "InstallPayload";
        public const string ModsFolderName = "Mods";
        public const string DisabledModsFolderName = "disabled";
        public const string SettingsFileName = "settings.json";

        public const string GameExecutableName = "DustAET.exe";
        public const string RealGameExecutableName = "DustAET.real.exe";
        public const string LauncherExecutableName = "Asher.Launcher.exe";

        public static string GetAppBaseDirectory() =>
            AppDomain.CurrentDomain.BaseDirectory;

        public static string GetLocalSettingsPath() =>
            Path.Combine(GetAppBaseDirectory(), SettingsFileName);

        public static string GetRuntimeFolderPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, RuntimeFolderName);

        public static string GetManagerFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), ManagerFolderName);

        public static string GetBackupFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), BackupFolderName);

        public static string GetPatchesFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), PatchesFolderName);

        public static string GetModsFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), ModsFolderName);

        public static string GetDisabledModsFolderPath(string gameFolderPath) =>
            Path.Combine(GetModsFolderPath(gameFolderPath), DisabledModsFolderName);

        public static string GetInstallPayloadPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), InstallPayloadFolderName);

        public static string? TryGetGameFolderFromManagerLocation()
        {
            var baseDir = GetAppBaseDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Path.GetFileName(baseDir).Equals(ManagerFolderName, StringComparison.OrdinalIgnoreCase))
                return null;

            var parentDir = Directory.GetParent(baseDir)?.FullName;
            if (parentDir == null)
                return null;

            if (Path.GetFileName(parentDir).Equals(RuntimeFolderName, StringComparison.OrdinalIgnoreCase))
            {
                var gameDir = Directory.GetParent(parentDir)?.FullName;
                return gameDir != null && IsValidGameFolder(gameDir) ? gameDir : null;
            }

            return IsValidGameFolder(parentDir) ? parentDir : null;
        }

        public static bool IsValidGameFolder(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
                return false;

            return File.Exists(Path.Combine(gameFolderPath, GameExecutableName))
                || (File.Exists(Path.Combine(gameFolderPath, RealGameExecutableName))
                    && Directory.Exists(GetRuntimeFolderPath(gameFolderPath)));
        }

        public static bool IsAsherInstalledIn(string gameFolderPath)
        {
            if (!IsValidGameFolder(gameFolderPath))
                return false;

            return File.Exists(Path.Combine(gameFolderPath, RealGameExecutableName))
                && Directory.Exists(GetRuntimeFolderPath(gameFolderPath));
        }

        public static void MigrateLegacyLayout(string gameFolderPath)
        {
            Directory.CreateDirectory(GetRuntimeFolderPath(gameFolderPath));

            TryMoveDirectory(
                Path.Combine(gameFolderPath, ManagerFolderName),
                GetManagerFolderPath(gameFolderPath));

            TryMoveDirectory(
                Path.Combine(gameFolderPath, BackupFolderName),
                GetBackupFolderPath(gameFolderPath));

            TryMoveDirectory(
                Path.Combine(gameFolderPath, PatchesFolderName),
                GetPatchesFolderPath(gameFolderPath));
        }

        private static void TryMoveDirectory(string source, string destination)
        {
            if (!Directory.Exists(source) || Directory.Exists(destination))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            Directory.Move(source, destination);
        }
    }
}
