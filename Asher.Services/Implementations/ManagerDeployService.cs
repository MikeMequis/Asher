using Asher.Core;
using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Services.Implementations
{
    public class ManagerDeployService : IManagerDeployService
    {
        private const string PayloadFolderName = "ManagerPayload";
        private const string PendingMarkerFileName = ".pending";
        private const string ManagerExeName = "Asher.App.exe";

        private static readonly string[] PayloadExcludedFiles =
        {
            AsherPaths.SettingsFileName,
            AsherPaths.GameExecutableName,
            AsherPaths.RealGameExecutableName,
            AsherPaths.LauncherExecutableName,
            "Asher.Runtime.dll",
            "Asher.SDK.dll",
            "0Harmony.dll"
        };

        public string GetPayloadFolderPath(string gameFolderPath) =>
            Path.Combine(AsherPaths.GetRuntimeFolderPath(gameFolderPath), PayloadFolderName);

        public bool IsRunningFromManagerOf(string gameFolderPath)
        {
            var detectedGamePath = AsherPaths.TryGetGameFolderFromManagerLocation();
            if (string.IsNullOrWhiteSpace(detectedGamePath))
                return false;

            return string.Equals(
                Path.GetFullPath(detectedGamePath),
                Path.GetFullPath(gameFolderPath),
                StringComparison.OrdinalIgnoreCase);
        }

        public bool ShouldDeferDeploy(string gameFolderPath)
        {
            if (IsRunningFromManagerOf(gameFolderPath))
                return true;

            return IsManagerRunningAt(AsherPaths.GetManagerFolderPath(gameFolderPath));
        }

        public bool HasPendingPayload(string gameFolderPath)
        {
            var markerPath = Path.Combine(GetPayloadFolderPath(gameFolderPath), PendingMarkerFileName);
            return File.Exists(markerPath);
        }

        public void StagePayload(string sourceFolder, string gameFolderPath)
        {
            var payloadFolder = GetPayloadFolderPath(gameFolderPath);
            if (Directory.Exists(payloadFolder))
                Directory.Delete(payloadFolder, true);

            Directory.CreateDirectory(payloadFolder);
            CopyDirectory(sourceFolder, payloadFolder);
            File.WriteAllText(Path.Combine(payloadFolder, PendingMarkerFileName), DateTime.UtcNow.ToString("O"));
        }

        public void DeployImmediate(string sourceFolder, string gameFolderPath)
        {
            var managerFolder = AsherPaths.GetManagerFolderPath(gameFolderPath);
            Directory.CreateDirectory(managerFolder);
            CopyDirectory(sourceFolder, managerFolder);
        }

        public void ApplyPendingPayload(string gameFolderPath)
        {
            var payloadFolder = GetPayloadFolderPath(gameFolderPath);
            if (!HasPendingPayload(gameFolderPath))
                return;

            var managerFolder = AsherPaths.GetManagerFolderPath(gameFolderPath);
            Directory.CreateDirectory(managerFolder);
            CopyDirectory(payloadFolder, managerFolder, skipMarkerFile: true);

            try
            {
                Directory.Delete(payloadFolder, true);
            }
            catch
            {
                try
                {
                    File.Delete(Path.Combine(payloadFolder, PendingMarkerFileName));
                }
                catch
                {
                    // Best effort cleanup.
                }
            }
        }

        private static bool IsManagerRunningAt(string managerFolderPath)
        {
            var managerExePath = Path.GetFullPath(Path.Combine(managerFolderPath, ManagerExeName));
            var currentProcessId = Process.GetCurrentProcess().Id;

            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ManagerExeName)))
            {
                if (process.Id == currentProcessId)
                    continue;

                try
                {
                    var runningPath = process.MainModule?.FileName;
                    if (runningPath != null
                        && string.Equals(Path.GetFullPath(runningPath), managerExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Access denied for some processes.
                }
            }

            return false;
        }

        private static void CopyDirectory(string sourceFolder, string destinationFolder, bool skipMarkerFile = false)
        {
            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                var fileName = Path.GetFileName(file);
                if (skipMarkerFile && fileName.Equals(PendingMarkerFileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ShouldSkipPayloadFile(fileName))
                    continue;

                var destPath = Path.Combine(destinationFolder, fileName);
                if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
                    continue;

                Directory.CreateDirectory(destinationFolder);
                File.Copy(file, destPath, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(sourceFolder))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName.Equals(AsherPaths.RuntimeFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(AsherPaths.BackupFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(AsherPaths.ManagerFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(PayloadFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(AsherPaths.DefaultModsFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(AsherPaths.InstallPayloadFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CopyDirectory(directory, Path.Combine(destinationFolder, directoryName), skipMarkerFile);
            }
        }

        private static bool ShouldSkipPayloadFile(string fileName) =>
            PayloadExcludedFiles.Any(excluded =>
                fileName.Equals(excluded, StringComparison.OrdinalIgnoreCase));
    }
}
