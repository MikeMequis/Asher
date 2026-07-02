using Asher.Core;
using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Services.Implementations
{
    public class ManagerLaunchService : IManagerLaunchService
    {
        private const string ManagerExeName = "Asher.App.exe";

        public string GetInstalledManagerPath(string gameFolderPath) =>
            Path.Combine(AsherPaths.GetManagerFolderPath(gameFolderPath), ManagerExeName);

        public bool ShouldRelaunchAfterInstall(string gameFolderPath)
        {
            var installedPath = GetInstalledManagerPath(gameFolderPath);
            if (!File.Exists(installedPath))
                return false;

            var currentPath = Path.Combine(
                AsherPaths.GetAppBaseDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                ManagerExeName);

            return !string.Equals(
                Path.GetFullPath(currentPath),
                Path.GetFullPath(installedPath),
                StringComparison.OrdinalIgnoreCase);
        }

        public bool TryRelaunchManager(string gameFolderPath, out string? errorMessage)
        {
            errorMessage = null;
            var installedPath = GetInstalledManagerPath(gameFolderPath);

            if (!File.Exists(installedPath))
            {
                errorMessage = $"Manager not found: {installedPath}";
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = installedPath,
                    WorkingDirectory = Path.GetDirectoryName(installedPath),
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
