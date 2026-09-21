using Asher.Core;
using Asher.Services.Interfaces;
using System.Diagnostics;
using System.Text;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Windows launcher-swap layout: the game executable is replaced by Asher.Launcher.exe
    /// and the original is preserved as DustAET.real.exe. Moved verbatim from
    /// GameInstallationService so behavior is unchanged.
    /// </summary>
    public sealed class WindowsGameExecutableLayout : IGameExecutableLayout
    {
        private const string OriginalExeName = "DustAET.exe";
        private const string BackupExeName = "DustAET.real.exe";
        private const string LauncherExeName = "Asher.Launcher.exe";

        public bool IsInstalled(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
                return false;

            return File.Exists(Path.Combine(gameFolderPath, BackupExeName));
        }

        public bool HasRestorableBackup(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
                return false;

            var backupCopyPath = Path.Combine(
                AsherPaths.GetBackupFolderPath(gameFolderPath),
                OriginalExeName);

            if (File.Exists(backupCopyPath))
                return true;

            return File.Exists(Path.Combine(gameFolderPath, BackupExeName));
        }

        public void CreateBackup(string gameFolderPath, string originalExePath)
        {
            var backupFolder = AsherPaths.GetBackupFolderPath(gameFolderPath);

            if (!Directory.Exists(backupFolder))
                Directory.CreateDirectory(backupFolder);

            var backupExePath = Path.Combine(backupFolder, OriginalExeName);
            File.Copy(originalExePath, backupExePath, overwrite: true);

            var metadataPath = Path.Combine(backupFolder, "backup_info.txt");
            File.WriteAllText(metadataPath,
                $"Backup criado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Versão do jogo: {FileVersionInfo.GetVersionInfo(originalExePath).FileVersion}\n" +
                $"Arquivo original: {OriginalExeName}");
        }

        public void RenameOriginalExecutable(string gameFolderPath)
        {
            var originalExePath = Path.Combine(gameFolderPath, OriginalExeName);
            var backupExePath = Path.Combine(gameFolderPath, BackupExeName);

            if (File.Exists(backupExePath))
                File.Delete(backupExePath);

            File.Move(originalExePath, backupExePath);
        }

        public void InstallLauncher(string gameFolderPath, string launcherSourcePath)
        {
            var launcherDest = Path.Combine(gameFolderPath, OriginalExeName);

            if (!File.Exists(launcherSourcePath))
                throw new FileNotFoundException($"Asher Launcher não encontrado: {launcherSourcePath}");

            File.Copy(launcherSourcePath, launcherDest, overwrite: true);

            var launcherConfigSource = launcherSourcePath + ".config";
            if (File.Exists(launcherConfigSource))
                File.Copy(launcherConfigSource, launcherDest + ".config", overwrite: true);
        }

        public void RestoreOriginalExecutable(string gameFolderPath)
        {
            var restoredExePath = Path.Combine(gameFolderPath, OriginalExeName);
            var backupCopyPath = Path.Combine(
                AsherPaths.GetBackupFolderPath(gameFolderPath),
                OriginalExeName);
            var renamedOriginalPath = Path.Combine(gameFolderPath, BackupExeName);

            if (File.Exists(backupCopyPath))
            {
                File.Copy(backupCopyPath, restoredExePath, overwrite: true);
            }
            else if (File.Exists(renamedOriginalPath))
            {
                if (File.Exists(restoredExePath))
                    File.Delete(restoredExePath);

                File.Move(renamedOriginalPath, restoredExePath);
            }
            else
            {
                throw new FileNotFoundException(
                    "Nenhum backup restaurável encontrado em Asher.Backup ou DustAET.real.exe");
            }

            if (File.Exists(renamedOriginalPath))
                File.Delete(renamedOriginalPath);
        }

        public void RemoveLauncherFiles(string gameFolderPath)
        {
            var launcherPath = Path.Combine(gameFolderPath, OriginalExeName);
            if (File.Exists(launcherPath))
                File.Delete(launcherPath);

            var launcherConfigPath = launcherPath + ".config";
            if (File.Exists(launcherConfigPath))
                File.Delete(launcherConfigPath);
        }

        public void RemoveInstalledMarker(string gameFolderPath)
        {
            TryForceDelete(Path.Combine(gameFolderPath, BackupExeName));
        }

        public void VerifyInstalled(string gameFolderPath)
        {
            var launcherPath = Path.Combine(gameFolderPath, OriginalExeName);
            if (!File.Exists(launcherPath))
                throw new InvalidOperationException("Launcher não foi instalado corretamente");

            var backupExePath = Path.Combine(gameFolderPath, BackupExeName);
            if (!File.Exists(backupExePath))
                throw new InvalidOperationException("Executável original não foi renomeado");
        }

        public void InstallRecoveryHelper(string gameFolderPath)
        {
            Directory.CreateDirectory(gameFolderPath);

            var ps1Path = AsherPaths.GetEmergencyUninstallPowerShellPath(gameFolderPath);
            var cmdPath = AsherPaths.GetEmergencyUninstallCmdPath(gameFolderPath);

            File.WriteAllText(ps1Path, BuildEmergencyUninstallPowerShell(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.WriteAllText(cmdPath, BuildEmergencyUninstallCmd(), Encoding.ASCII);

            // Remove helpers left inside Asher\ from older installs.
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            TryDeleteFile(Path.Combine(asherFolder, AsherPaths.EmergencyUninstallPowerShellName));
            TryDeleteFile(Path.Combine(asherFolder, AsherPaths.EmergencyUninstallScriptName));
        }

        public void RemoveRecoveryHelper(string gameFolderPath)
        {
            TryDeleteFile(AsherPaths.GetEmergencyUninstallCmdPath(gameFolderPath));
            TryDeleteFile(AsherPaths.GetEmergencyUninstallPowerShellPath(gameFolderPath));
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best effort.
            }
        }

        private static void TryForceDelete(string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);

                File.Delete(path);
            }
            catch
            {
                // Caller verifies markers via IsInstalled after cleanup.
            }
        }

        private static string BuildEmergencyUninstallCmd() =>
            "@echo off\r\n" +
            "setlocal\r\n" +
            "cd /d \"%~dp0\"\r\n" +
            "echo Asher emergency uninstall\r\n" +
            "echo This restores DustAET.exe, removes the Asher folder, then deletes this helper.\r\n" +
            "echo.\r\n" +
            "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"%~dp0" +
            AsherPaths.EmergencyUninstallPowerShellName +
            "\"\r\n" +
            "exit /b %ERRORLEVEL%\r\n";

        private static string BuildEmergencyUninstallPowerShell()
        {
            // Scripts live in <game>\ (next to DustAET.exe), not inside Asher\.
            return """
$ErrorActionPreference = 'Stop'
$gameDir = $PSScriptRoot
$asherDir = Join-Path $gameDir 'Asher'
$gameExe = Join-Path $gameDir 'DustAET.exe'
$realExe = Join-Path $gameDir 'DustAET.real.exe'
$backupExe = Join-Path $asherDir 'Asher.Backup\DustAET.exe'
$helperPs1 = Join-Path $gameDir 'Uninstall-Asher.ps1'
$helperCmd = Join-Path $gameDir 'Uninstall-Asher.cmd'

Write-Host 'Asher emergency uninstall'
Write-Host "Game folder: $gameDir"
$confirm = Read-Host 'Remove Asher and restore the original DustAET.exe? (Y/N)'
if ($confirm -notmatch '^[Yy]') { Write-Host 'Cancelled.'; exit 1 }

if (-not (Test-Path -LiteralPath $asherDir) -and -not (Test-Path -LiteralPath $realExe) -and -not (Test-Path -LiteralPath $backupExe)) {
  Write-Host 'ERROR: Asher does not appear to be installed here.'
  Read-Host 'Press Enter to close'
  exit 1
}

if (-not (Test-Path -LiteralPath $backupExe) -and -not (Test-Path -LiteralPath $realExe)) {
  Write-Host 'ERROR: No restorable backup found (Asher\Asher.Backup\DustAET.exe or DustAET.real.exe).'
  Read-Host 'Press Enter to close'
  exit 1
}

if (Test-Path -LiteralPath $gameExe) { Remove-Item -LiteralPath $gameExe -Force }
$configPath = $gameExe + '.config'
if (Test-Path -LiteralPath $configPath) { Remove-Item -LiteralPath $configPath -Force }

if (Test-Path -LiteralPath $backupExe) {
  Copy-Item -LiteralPath $backupExe -Destination $gameExe -Force
} else {
  Move-Item -LiteralPath $realExe -Destination $gameExe -Force
}

if (Test-Path -LiteralPath $realExe) { Remove-Item -LiteralPath $realExe -Force }

# Helpers are outside Asher\, so the folder can be removed immediately.
if (Test-Path -LiteralPath $asherDir) {
  Remove-Item -LiteralPath $asherDir -Recurse -Force
}

# After this window closes, delete Uninstall-Asher.cmd / .ps1 (locked while we run).
$waitPid = $PID
$cmdEsc = $helperCmd.Replace("'", "''")
$ps1Esc = $helperPs1.Replace("'", "''")
$cleanup = Join-Path $env:TEMP ("asher-helper-self-delete-{0}.ps1" -f [guid]::NewGuid().ToString('N'))
$cleanupEsc = $cleanup.Replace("'", "''")
@(
  '$ErrorActionPreference = ''SilentlyContinue'''
  "while (Get-Process -Id $waitPid -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 400 }"
  'Start-Sleep -Milliseconds 600'
  "for (`$i = 0; `$i -lt 15; `$i++) {"
  "  if (-not (Test-Path -LiteralPath '$cmdEsc') -and -not (Test-Path -LiteralPath '$ps1Esc')) { break }"
  "  Remove-Item -LiteralPath '$cmdEsc' -Force -ErrorAction SilentlyContinue"
  "  Remove-Item -LiteralPath '$ps1Esc' -Force -ErrorAction SilentlyContinue"
  '  Start-Sleep -Milliseconds 400'
  '}'
  "Remove-Item -LiteralPath '$cleanupEsc' -Force -ErrorAction SilentlyContinue"
) | Set-Content -LiteralPath $cleanup -Encoding ASCII

Start-Process -FilePath powershell.exe -ArgumentList @(
  '-NoProfile','-ExecutionPolicy','Bypass','-WindowStyle','Hidden','-File', $cleanup
) -WindowStyle Hidden | Out-Null

Write-Host 'Asher folder removed and original DustAET.exe restored.'
Write-Host 'This uninstall helper will delete itself after you close this window.'
Read-Host 'Press Enter to close'
exit 0
""";
        }
    }
}
