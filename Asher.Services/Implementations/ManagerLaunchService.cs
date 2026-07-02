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

        public bool TryRestartCurrentManager(out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    errorMessage = "Não foi possível localizar o executável atual.";
                    return false;
                }

                var workingDirectory = Path.GetDirectoryName(exePath) ?? AsherPaths.GetAppBaseDirectory();
                var currentProcessId = Process.GetCurrentProcess().Id;
                var restartScriptPath = Path.Combine(
                    Path.GetTempPath(),
                    $"asher-restart-{currentProcessId}.cmd");

                File.WriteAllText(restartScriptPath,
                    $"""
                    @echo off
                    :wait
                    tasklist /FI "PID eq {currentProcessId}" 2>nul | find "{currentProcessId}" >nul
                    if not errorlevel 1 (
                        timeout /t 1 /nobreak >nul
                        goto wait
                    )
                    start "" "{exePath}"
                    del "%~f0"
                    """);

                Process.Start(new ProcessStartInfo
                {
                    FileName = restartScriptPath,
                    WorkingDirectory = workingDirectory,
                    CreateNoWindow = true,
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
