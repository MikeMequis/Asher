using System.IO;

namespace Asher.Core
{
    public static class AsherPaths
    {
        public const string RuntimeFolderName = "Asher";
        public const string ManagerFolderName = "Asher.App";
        public const string BackupFolderName = "Asher.Backup";
        public const string PatchesFolderName = "patches";
        public const string DefaultModsFolderName = "DefaultMods";
        public const string InstallPayloadFolderName = "InstallPayload";
        public const string HostInstallPayloadFolderName = "install-payload";
        public const string ModsFolderName = "Mods";
        public const string DisabledModsFolderName = "disabled";
        public const string SettingsFileName = "settings.json";
        public const string EmergencyUninstallScriptName = "Uninstall-Asher.cmd";
        public const string EmergencyUninstallPowerShellName = "Uninstall-Asher.ps1";

        public const string GameExecutableName = "DustAET.exe";
        public const string RealGameExecutableName = "DustAET.real.exe";
        public const string LauncherExecutableName = "Asher.Launcher.exe";

        public static string GetAppBaseDirectory() =>
            AppDomain.CurrentDomain.BaseDirectory;

        public static string GetLocalSettingsPath() =>
            Path.Combine(GetAppBaseDirectory(), SettingsFileName);

        public static string GetRuntimeFolderPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, RuntimeFolderName);

        public static string GetEmergencyUninstallCmdPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, EmergencyUninstallScriptName);

        public static string GetEmergencyUninstallPowerShellPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, EmergencyUninstallPowerShellName);

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

            var realExePath = Path.Combine(gameFolderPath, RealGameExecutableName);
            if (!File.Exists(realExePath))
                return false;

            return HasActiveRuntimeFiles(gameFolderPath);
        }

        private static bool HasActiveRuntimeFiles(string gameFolderPath)
        {
            var asherFolder = GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return false;

            return File.Exists(Path.Combine(asherFolder, "Asher.Runtime.dll"))
                   || File.Exists(Path.Combine(asherFolder, "Asher.SDK.dll"))
                   || File.Exists(Path.Combine(asherFolder, "0Harmony.dll"));
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
