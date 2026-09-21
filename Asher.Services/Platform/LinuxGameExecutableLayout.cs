using Asher.Core;
using Asher.Core.Platform;
using Asher.Services.Interfaces;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Linux has no executable swap: DustAET stays the native ELF and Asher attaches through
    /// libasher_bootstrap.so. The install marker is therefore the deployed bootstrap library,
    /// and there is nothing to back up or restore.
    /// </summary>
    public sealed class LinuxGameExecutableLayout : IGameExecutableLayout
    {
        private readonly IPlatformInfo _platform;

        public LinuxGameExecutableLayout(IPlatformInfo platform)
        {
            _platform = platform;
        }

        public bool IsInstalled(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
                return false;

            return File.Exists(AsherPaths.GetBootstrapLibraryPath(gameFolderPath, _platform));
        }

        /// <summary>DustAET is never modified on Linux, so there is nothing to restore.</summary>
        public bool HasRestorableBackup(string gameFolderPath) => false;

        public void CreateBackup(string gameFolderPath, string originalExePath)
        {
            // Nothing is overwritten on Linux; no backup required.
        }

        public void RenameOriginalExecutable(string gameFolderPath)
        {
            // No executable swap on Linux.
        }

        public void InstallLauncher(string gameFolderPath, string launcherSourcePath)
        {
            // No executable swap on Linux; libasher_bootstrap.so is deployed by IRuntimeDeployment.
        }

        public void RestoreOriginalExecutable(string gameFolderPath)
        {
            // The original DustAET is untouched.
        }

        public void RemoveLauncherFiles(string gameFolderPath)
        {
            // No launcher files to remove.
        }

        public void RemoveInstalledMarker(string gameFolderPath)
        {
            // The bootstrap library is removed by IRuntimeDeployment.CleanRuntimeFiles.
        }

        public void VerifyInstalled(string gameFolderPath)
        {
            var bootstrapPath = AsherPaths.GetBootstrapLibraryPath(gameFolderPath, _platform);
            if (!File.Exists(bootstrapPath))
                throw new InvalidOperationException(
                    $"Bootstrap do Asher não foi instalado corretamente: {bootstrapPath}");
        }

        public void InstallRecoveryHelper(string gameFolderPath)
        {
            // No user-runnable recovery script on Linux (SupportsRecoveryHelper = false).
        }

        public void RemoveRecoveryHelper(string gameFolderPath)
        {
            // No user-runnable recovery script on Linux.
        }
    }
}
