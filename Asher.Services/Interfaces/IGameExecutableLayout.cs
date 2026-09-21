namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Platform-specific handling of the game's executable slot: the launcher swap, its
    /// backup/restore, install markers, and the (platform-dependent) recovery helper.
    /// The generic install orchestration stays in
    /// <see cref="Implementations.GameInstallationService"/>.
    /// </summary>
    public interface IGameExecutableLayout
    {
        /// <summary>True when the installed layout marker is present.</summary>
        bool IsInstalled(string gameFolderPath);

        /// <summary>True when uninstall can safely revert the executable slot.</summary>
        bool HasRestorableBackup(string gameFolderPath);

        void CreateBackup(string gameFolderPath, string originalExePath);

        /// <summary>Windows: renames DustAET.exe to DustAET.real.exe. Linux: no-op.</summary>
        void RenameOriginalExecutable(string gameFolderPath);

        /// <summary>Windows: copies Asher.Launcher.exe over DustAET.exe. Linux: no-op.</summary>
        void InstallLauncher(string gameFolderPath, string launcherSourcePath);

        void RestoreOriginalExecutable(string gameFolderPath);

        void RemoveLauncherFiles(string gameFolderPath);

        /// <summary>Removes the installed layout marker so a reinstall is not blocked.</summary>
        void RemoveInstalledMarker(string gameFolderPath);

        /// <summary>Verifies the executable slot matches the installed layout; throws when invalid.</summary>
        void VerifyInstalled(string gameFolderPath);

        void InstallRecoveryHelper(string gameFolderPath);

        void RemoveRecoveryHelper(string gameFolderPath);
    }
}
