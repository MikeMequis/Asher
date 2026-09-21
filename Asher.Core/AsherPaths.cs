using Asher.Core.Platform;
using System.IO;

namespace Asher.Core
{
    /// <summary>
    /// Central path vocabulary. Names are grouped by what they are relative to so the
    /// platform-varying ones are easy to audit:
    ///  - game-relative: layout written next to the game executable (same on every platform)
    ///  - Asher-installation-relative: files shipped by the manager/payload
    ///  - user/application-data-relative: per-user settings
    ///  - platform-specific: executable/library/helper names resolved from <see cref="PlatformInfo"/>
    /// </summary>
    public static class AsherPaths
    {
        // --- Game-relative layout (identical across platforms) ---
        public const string RuntimeFolderName = "Asher";
        public const string BackupFolderName = "Asher.Backup";
        public const string PatchesFolderName = "patches";
        public const string ModsFolderName = "Mods";
        public const string DisabledModsFolderName = "disabled";
        public const string LogsFolderName = "AsherLogs";

        // --- Asher-installation-relative (manager/payload side) ---
        public const string ManagerFolderName = "Asher.App";
        public const string DefaultModsFolderName = "DefaultMods";
        public const string InstallPayloadFolderName = "InstallPayload";
        public const string HostInstallPayloadFolderName = "install-payload";
        public const string SettingsFileName = "settings.json";
        public const string PortableMarkerFileName = "portable";

        // --- Recovery helper names (Windows only; see SupportsRecoveryHelper) ---
        public const string EmergencyUninstallScriptName = "Uninstall-Asher.cmd";
        public const string EmergencyUninstallPowerShellName = "Uninstall-Asher.ps1";

        // --- Platform-specific names (resolved from PlatformInfo.Current) ---
        public static string GameExecutableName => PlatformInfo.Current.GameExecutableName;

        public static string RealGameExecutableName => PlatformInfo.Current.RealGameExecutableName;

        public static string LauncherExecutableName => PlatformInfo.Current.LauncherExecutableName;

        public static string BootstrapLibraryName => PlatformInfo.Current.BootstrapLibraryName;

        public static IPlatformInfo Platform => PlatformInfo.Current;

        public static string GetAppBaseDirectory() =>
            AppDomain.CurrentDomain.BaseDirectory;

        public static string GetLocalSettingsPath() =>
            Path.Combine(GetAppBaseDirectory(), SettingsFileName);

        // --- Game-relative path builders ---
        public static string GetRuntimeFolderPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, RuntimeFolderName);

        public static string GetManagerFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), ManagerFolderName);

        public static string GetBackupFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), BackupFolderName);

        public static string GetPatchesFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), PatchesFolderName);

        public static string GetLogsFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), LogsFolderName);

        public static string GetModsFolderPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), ModsFolderName);

        public static string GetDisabledModsFolderPath(string gameFolderPath) =>
            Path.Combine(GetModsFolderPath(gameFolderPath), DisabledModsFolderName);

        public static string GetInstallPayloadPath(string gameFolderPath) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), InstallPayloadFolderName);

        /// <summary>Native bootstrap library inside the game's Asher/ folder (Linux).</summary>
        public static string GetBootstrapLibraryPath(string gameFolderPath) =>
            GetBootstrapLibraryPath(gameFolderPath, PlatformInfo.Current);

        public static string GetBootstrapLibraryPath(string gameFolderPath, IPlatformInfo platform) =>
            Path.Combine(GetRuntimeFolderPath(gameFolderPath), platform.BootstrapLibraryName);

        // --- Recovery helper paths (game folder root; Windows only) ---
        public static string GetEmergencyUninstallCmdPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, EmergencyUninstallScriptName);

        public static string GetEmergencyUninstallPowerShellPath(string gameFolderPath) =>
            Path.Combine(gameFolderPath, EmergencyUninstallPowerShellName);

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

        public static bool IsValidGameFolder(string gameFolderPath) =>
            IsValidGameFolder(gameFolderPath, PlatformInfo.Current);

        public static bool IsValidGameFolder(string gameFolderPath, IPlatformInfo platform)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
                return false;

            return File.Exists(Path.Combine(gameFolderPath, platform.GameExecutableName))
                || (HasInstalledMarker(gameFolderPath, platform)
                    && Directory.Exists(GetRuntimeFolderPath(gameFolderPath)));
        }

        public static bool IsAsherInstalledIn(string gameFolderPath) =>
            IsAsherInstalledIn(gameFolderPath, PlatformInfo.Current);

        public static bool IsAsherInstalledIn(string gameFolderPath, IPlatformInfo platform)
        {
            if (!IsValidGameFolder(gameFolderPath, platform))
                return false;

            if (!HasInstalledMarker(gameFolderPath, platform))
                return false;

            return HasActiveRuntimeFiles(gameFolderPath);
        }

        /// <summary>
        /// The marker that proves the executable slot was prepared by Asher:
        /// the renamed original on the launcher-swap model, or the bootstrap library on Linux.
        /// </summary>
        private static bool HasInstalledMarker(string gameFolderPath, IPlatformInfo platform)
        {
            if (platform.UsesLauncherSwap)
                return File.Exists(Path.Combine(gameFolderPath, platform.RealGameExecutableName));

            if (!string.IsNullOrEmpty(platform.BootstrapLibraryName))
                return File.Exists(GetBootstrapLibraryPath(gameFolderPath, platform));

            return true;
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
