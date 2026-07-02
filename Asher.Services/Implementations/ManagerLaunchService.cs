using Asher.Core;
using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Services.Implementations
{
    public class ManagerLaunchService : IManagerLaunchService
    {
        private const string ManagerExeName = "Asher.App.exe";
        private const string PayloadFolderName = "ManagerPayload";

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

        public bool TryFinishInstallWithPendingPayload(string gameFolderPath, out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                var managerFolder = AsherPaths.GetManagerFolderPath(gameFolderPath);
                var managerExe = Path.Combine(managerFolder, ManagerExeName);
                var payloadFolder = Path.Combine(
                    AsherPaths.GetRuntimeFolderPath(gameFolderPath),
                    PayloadFolderName);

                if (!Directory.Exists(payloadFolder))
                {
                    errorMessage = $"Pending payload not found: {payloadFolder}";
                    return false;
                }

                var currentProcessId = Process.GetCurrentProcess().Id;
                var scriptPath = Path.Combine(
                    Path.GetTempPath(),
                    $"asher-apply-payload-{currentProcessId}.cmd");

                File.WriteAllText(scriptPath,
                    $"""
                    @echo off
                    :wait
                    tasklist /FI "PID eq {currentProcessId}" 2>nul | find "{currentProcessId}" >nul
                    if not errorlevel 1 (
                        timeout /t 1 /nobreak >nul
                        goto wait
                    )
                    robocopy "{payloadFolder}" "{managerFolder}" /E /IS /IT /XF .pending /NFL /NDL /NJH /NJS /NC /NS /NP >nul
                    if exist "{payloadFolder}\.pending" del /F /Q "{payloadFolder}\.pending"
                    rd /S /Q "{payloadFolder}" 2>nul
                    start "" "{managerExe}"
                    del "%~f0"
                    """);

                Process.Start(new ProcessStartInfo
                {
                    FileName = scriptPath,
                    WorkingDirectory = managerFolder,
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
