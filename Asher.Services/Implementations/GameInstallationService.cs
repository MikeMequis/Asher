using Asher.Core;
using Asher.Core.Models;
using Asher.Services;
using Asher.Services.Interfaces;
using System.Reflection;
using System.Text;

namespace Asher.Services.Implementations
{
    public class GameInstallationService : IGameInstallationService
    {
        private const string OriginalExeName = AsherPaths.GameExecutableName;
        private const string BackupExeName = AsherPaths.RealGameExecutableName;
        private const string LauncherExeName = AsherPaths.LauncherExecutableName;
        private const string BackupFolderName = AsherPaths.BackupFolderName;
        private const string AsherFolderName = AsherPaths.RuntimeFolderName;
        private const string LogsFolderName = "AsherLogs";

        private readonly ISettingsService _settingsService;

        private static readonly string[] RequiredRuntimeFiles =
        {
            "Asher.Runtime.dll",
            "Asher.SDK.dll",
            "0Harmony.dll"
        };

        private static readonly string[] DefaultModFiles =
        {
            "Asher.Patching.DebugEnabler.dll",
            "Asher.Patching.IntroSkipper.dll",
            "Asher.Patching.GraphicsDeprofiler.dll",
            "Asher.Patching.MuteVoiceActing.dll",
            "Asher.Patching.OverheatDisabler.dll"
        };

        public GameInstallationService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<InstallationResult> InstallAsync(
            GameFolderInfo gameInfo,
            IProgress<InstallationProgress> progress)
        {
            if (gameInfo == null || !gameInfo.IsValid)
            {
                return new InstallationResult
                {
                    Success = false,
                    Message = "Informações do jogo inválidas"
                };
            }

            try
            {
                var gamePath = gameInfo.Path;
                var originalExePath = Path.Combine(gamePath, OriginalExeName);

                InstallFlowTrace.Log("InstallAsync start", $"path={gamePath}");

                if (IsInstalled(gamePath))
                {
                    var details = DescribeInstallMarkers(gamePath);
                    InstallFlowTrace.Log("InstallAsync blocked", details);
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "O Asher já está instalado neste jogo. Desinstale-o antes de instalar novamente.",
                        Details = DescribeInstallMarkers(gamePath)
                    };
                }

                await Task.Run(() => RemoveStaleInstallMarkers(gamePath));
                await Task.Run(() => AsherPaths.MigrateLegacyLayout(gamePath));

                InstallFlowTrace.Log("InstallAsync proceeding", DescribeInstallMarkers(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 10,
                    Message = "Criando backup dos arquivos originais...",
                    Details = "Preparando backup de segurança"
                });

                await Task.Run(() => CreateBackup(gamePath, originalExePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 20,
                    Message = "Backup criado com sucesso",
                    Details = $"Backup salvo em {BackupFolderName}/"
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 25,
                    Message = "Criando estrutura de pastas...",
                    Details = "Configurando diretórios do Asher"
                });

                await Task.Run(() => CreateFolderStructure(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 30,
                    Message = "Estrutura de pastas criada",
                    Details = "Pastas Asher/, Mods/ e AsherLogs/ criadas"
                });

                await Task.Run(() => InstallEmergencyUninstallHelper(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 35,
                    Message = "Copiando arquivos do runtime...",
                    Details = "Instalando Asher.Runtime.dll, Asher.SDK.dll, 0Harmony.dll"
                });

                await Task.Run(() => CopyRuntimeFiles(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 50,
                    Message = "Runtime instalado",
                    Details = "Arquivos core do Asher copiados"
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 55,
                    Message = "Instalando mods padrão...",
                    Details = "Copiando DebugEnabler, IntroSkipper, GraphicsDeprofiler"
                });

                var modsCopied = await Task.Run(() => CopyDefaultMods(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 65,
                    Message = modsCopied > 0 ? "Mods padrão instalados" : "Nenhum mod padrão no payload",
                    Details = modsCopied > 0
                        ? $"{modsCopied} mod(s) copiado(s) para Asher/Mods/"
                        : "Payload sem DefaultMods — patches não carregarão até mods serem adicionados"
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 70,
                    Message = "Configurando executáveis...",
                    Details = "Renomeando DustAET.exe → DustAET.real.exe"
                });

                await Task.Run(() => RenameOriginalExecutable(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 75,
                    Message = "Executável original preservado",
                    Details = "DustAET.real.exe criado"
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 80,
                    Message = "Instalando Asher Launcher...",
                    Details = "Copiando Asher.Launcher.exe → DustAET.exe"
                });

                await Task.Run(() => InstallLauncher(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 90,
                    Message = "Launcher instalado",
                    Details = "Novo DustAET.exe configurado"
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 95,
                    Message = "Verificando instalação...",
                    Details = "Validando arquivos instalados"
                });

                await Task.Run(() => VerifyInstallation(gamePath));

                InstallFlowTrace.Log("InstallAsync complete", $"path={gamePath}");

                progress?.Report(new InstallationProgress
                {
                    Percentage = 100,
                    Message = "Instalação concluída!",
                    Details = "O Asher está pronto para uso"
                });

                return new InstallationResult
                {
                    Success = true,
                    Message = modsCopied > 0
                        ? "Instalação concluída com sucesso!"
                        : "Instalação concluída, mas nenhum mod padrão foi copiado. Reinstale após incluir DefaultMods no payload.",
                    GameFolderPath = gamePath
                };
            }
            catch (Exception ex)
            {
                return new InstallationResult
                {
                    Success = false,
                    Message = $"Erro durante a instalação: {ex.Message}",
                    Error = ex
                };
            }
        }

        public async Task<InstallationResult> UninstallAsync(
            string gameFolderPath,
            IProgress<InstallationProgress> progress)
        {
            try
            {
                InstallFlowTrace.Log("UninstallAsync start", $"path={gameFolderPath} installed={IsInstalled(gameFolderPath)}");

                if (!IsInstalled(gameFolderPath))
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "O Asher não está instalado neste jogo"
                    };
                }

                if (!HasRestorableBackup(gameFolderPath))
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "Nenhum backup restaurável foi encontrado em Asher/Asher.Backup"
                    };
                }

                progress?.Report(new InstallationProgress
                {
                    Percentage = 10,
                    Message = "Removendo Asher Launcher...",
                    Details = "Preparando restauração do executável original"
                });

                await Task.Run(() => RemoveLauncherFiles(gameFolderPath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 35,
                    Message = "Restaurando executável original...",
                    Details = $"Restaurando {OriginalExeName} a partir do backup"
                });

                await Task.Run(() => RestoreOriginalExecutable(gameFolderPath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 65,
                    Message = "Removendo arquivos do runtime...",
                    Details = "Limpando mods, logs e DLLs do Asher"
                });

                await Task.Run(() => CleanRuntimeFiles(gameFolderPath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 90,
                    Message = "Verificando remoção...",
                    Details = "Confirmando que os marcadores de instalação foram removidos"
                });

                await Task.Run(() => EnsureInstallMarkersRemoved(gameFolderPath));

                var stillInstalled = IsInstalled(gameFolderPath);
                InstallFlowTrace.Log(
                    "UninstallAsync verify",
                    $"path={gameFolderPath} stillInstalled={stillInstalled} {DescribeInstallMarkers(gameFolderPath)}");

                if (stillInstalled)
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "A desinstalação não removeu todos os marcadores. O Asher ainda parece instalado.",
                        Details = DescribeInstallMarkers(gameFolderPath)
                    };
                }

                progress?.Report(new InstallationProgress
                {
                    Percentage = 100,
                    Message = "Restauração concluída",
                    Details = "O jogo foi restaurado ao estado original"
                });

                return new InstallationResult
                {
                    Success = true,
                    Message = "Asher removido e jogo restaurado com sucesso",
                    GameFolderPath = gameFolderPath
                };
            }
            catch (Exception ex)
            {
                InstallFlowTrace.Log("UninstallAsync error", ex.Message);
                return new InstallationResult
                {
                    Success = false,
                    Message = $"Erro durante a desinstalação: {ex.Message}",
                    Error = ex
                };
            }
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

        public bool IsInstalled(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
                return false;

            var realExePath = Path.Combine(gameFolderPath, BackupExeName);
            var hasRealExe = File.Exists(realExePath);
            var hasRuntime = HasActiveRuntime(gameFolderPath);
            var installed = hasRealExe && hasRuntime;

            InstallFlowTrace.Log(
                "IsInstalled",
                $"path={gameFolderPath} realExe={hasRealExe} runtime={hasRuntime} => {installed}");

            return installed;
        }

        public string DescribeInstallState(string gameFolderPath) =>
            DescribeInstallMarkers(gameFolderPath);

        private static bool HasActiveRuntime(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return false;

            return RequiredRuntimeFiles.Any(fileName =>
                File.Exists(Path.Combine(asherFolder, fileName)));
        }

        private static string DescribeInstallMarkers(string gameFolderPath)
        {
            var markers = new List<string>();

            var realExePath = Path.Combine(gameFolderPath, BackupExeName);
            if (File.Exists(realExePath))
                markers.Add(BackupExeName);

            var asherFolder = Path.Combine(gameFolderPath, AsherFolderName);
            if (Directory.Exists(asherFolder))
            {
                if (HasActiveRuntime(gameFolderPath))
                    markers.Add($"pasta {AsherFolderName}/ (runtime ativo)");
                else
                    markers.Add($"pasta {AsherFolderName}/ (resíduo — não bloqueia reinstalação)");
            }

            if (markers.Count == 0)
                return "Nenhum marcador de instalação ativo encontrado.";

            return $"Marcadores encontrados: {string.Join(", ", markers)}";
        }

        #region Private Methods

        private void CreateBackup(string gamePath, string originalExePath)
        {
            var backupFolder = AsherPaths.GetBackupFolderPath(gamePath);

            if (!Directory.Exists(backupFolder))
                Directory.CreateDirectory(backupFolder);

            var backupExePath = Path.Combine(backupFolder, OriginalExeName);
            File.Copy(originalExePath, backupExePath, overwrite: true);

            var metadataPath = Path.Combine(backupFolder, "backup_info.txt");
            File.WriteAllText(metadataPath,
                $"Backup criado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Versão do jogo: {System.Diagnostics.FileVersionInfo.GetVersionInfo(originalExePath).FileVersion}\n" +
                $"Arquivo original: {OriginalExeName}");
        }

        private void CreateFolderStructure(string gamePath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            Directory.CreateDirectory(asherFolder);
            Directory.CreateDirectory(AsherPaths.GetModsFolderPath(gamePath));
            Directory.CreateDirectory(Path.Combine(asherFolder, LogsFolderName));
        }

        /// <summary>
        /// Writes a double-clickable emergency uninstall helper next to DustAET.exe
        /// (game folder root — outside Asher/) so it can delete Asher\ while running,
        /// then self-delete after the user closes the window.
        /// </summary>
        private static void InstallEmergencyUninstallHelper(string gamePath)
        {
            Directory.CreateDirectory(gamePath);

            var ps1Path = AsherPaths.GetEmergencyUninstallPowerShellPath(gamePath);
            var cmdPath = AsherPaths.GetEmergencyUninstallCmdPath(gamePath);

            File.WriteAllText(ps1Path, BuildEmergencyUninstallPowerShell(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.WriteAllText(cmdPath, BuildEmergencyUninstallCmd(), Encoding.ASCII);

            // Remove helpers left inside Asher\ from older installs.
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            TryDeleteFile(Path.Combine(asherFolder, AsherPaths.EmergencyUninstallPowerShellName));
            TryDeleteFile(Path.Combine(asherFolder, AsherPaths.EmergencyUninstallScriptName));
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

        private void CopyRuntimeFiles(string gamePath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            var sourceFolder = ResolveInstallSourceFolder(gamePath);

            foreach (var fileName in RequiredRuntimeFiles)
            {
                var sourcePath = Path.Combine(sourceFolder, fileName);
                var destPath = Path.Combine(asherFolder, fileName);

                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException($"Arquivo necessário não encontrado: {fileName}", sourcePath);

                File.Copy(sourcePath, destPath, overwrite: true);
            }

            ValidateHarmonyAssembly(Path.Combine(asherFolder, "0Harmony.dll"));
        }

        private static void ValidateHarmonyAssembly(string harmonyPath)
        {
            try
            {
                foreach (var reference in Assembly.ReflectionOnlyLoadFrom(harmonyPath).GetReferencedAssemblies())
                {
                    if (reference.Name == "System.Runtime" && reference.Version.Major >= 5)
                    {
                        throw new InvalidOperationException(
                            "0Harmony.dll incompatível: foi copiada de um target .NET moderno (net8/net10). " +
                            "Use a versão net472 de packages\\Lib.Harmony.2.4.2\\lib\\net472.");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // Best-effort validation only.
            }
        }

        private int CopyDefaultMods(string gamePath)
        {
            var modsFolder = AsherPaths.GetModsFolderPath(gamePath);
            Directory.CreateDirectory(modsFolder);

            var copied = 0;
            foreach (var sourceFolder in GetDefaultModsSourceCandidates(gamePath))
            {
                if (!Directory.Exists(sourceFolder))
                    continue;

                foreach (var fileName in DefaultModFiles)
                {
                    var sourcePath = Path.Combine(sourceFolder, fileName);
                    if (!File.Exists(sourcePath))
                        continue;

                    File.Copy(sourcePath, Path.Combine(modsFolder, fileName), overwrite: true);
                    copied++;
                }

                if (copied > 0)
                    return copied;
            }

            return copied;
        }

        private IEnumerable<string> GetDefaultModsSourceCandidates(string gamePath)
        {
            foreach (var candidate in GetInstallSourceCandidates(gamePath))
            {
                yield return Path.Combine(candidate, AsherPaths.DefaultModsFolderName);
            }
        }

        private void RenameOriginalExecutable(string gamePath)
        {
            var originalExePath = Path.Combine(gamePath, OriginalExeName);
            var backupExePath = Path.Combine(gamePath, BackupExeName);

            if (File.Exists(backupExePath))
                File.Delete(backupExePath);

            File.Move(originalExePath, backupExePath);
        }

        private void InstallLauncher(string gamePath)
        {
            var sourceFolder = ResolveInstallSourceFolder(gamePath);
            var launcherSource = Path.Combine(sourceFolder, LauncherExeName);
            var launcherDest = Path.Combine(gamePath, OriginalExeName);

            if (!File.Exists(launcherSource))
                throw new FileNotFoundException($"Asher Launcher não encontrado: {launcherSource}");

            File.Copy(launcherSource, launcherDest, overwrite: true);

            var launcherConfigSource = launcherSource + ".config";
            if (File.Exists(launcherConfigSource))
                File.Copy(launcherConfigSource, launcherDest + ".config", overwrite: true);
        }

        private void VerifyInstallation(string gamePath)
        {
            var launcherPath = Path.Combine(gamePath, OriginalExeName);
            if (!File.Exists(launcherPath))
                throw new InvalidOperationException("Launcher não foi instalado corretamente");

            var backupExePath = Path.Combine(gamePath, BackupExeName);
            if (!File.Exists(backupExePath))
                throw new InvalidOperationException("Executável original não foi renomeado");

            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            if (!Directory.Exists(asherFolder))
                throw new InvalidOperationException("Pasta Asher não foi criada");

            foreach (var fileName in RequiredRuntimeFiles)
            {
                var filePath = Path.Combine(asherFolder, fileName);
                if (!File.Exists(filePath))
                    throw new InvalidOperationException($"Arquivo do runtime não encontrado: {fileName}");
            }
        }

        private static void RemoveLauncherFiles(string gameFolderPath)
        {
            var launcherPath = Path.Combine(gameFolderPath, OriginalExeName);
            if (File.Exists(launcherPath))
                File.Delete(launcherPath);

            var launcherConfigPath = launcherPath + ".config";
            if (File.Exists(launcherConfigPath))
                File.Delete(launcherConfigPath);
        }

        private static void RestoreOriginalExecutable(string gameFolderPath)
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

        /// <summary>
        /// Removes leftover install markers so a subsequent install is not blocked.
        /// Keeps Asher.Backup for safety.
        /// </summary>
        private static void EnsureInstallMarkersRemoved(string gameFolderPath)
        {
            TryForceDelete(Path.Combine(gameFolderPath, BackupExeName));

            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return;

            foreach (var file in Directory.GetFiles(asherFolder))
                TryForceDelete(file);

            foreach (var directory in Directory.GetDirectories(asherFolder))
            {
                var directoryName = Path.GetFileName(directory);
                if (ShouldPreserveAsherSubfolder(directoryName))
                    continue;

                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {
                    // Best effort — uninstall already reported runtime cleanup.
                }
            }
        }

        private static void RemoveStaleInstallMarkers(string gameFolderPath)
        {
            var realExePath = Path.Combine(gameFolderPath, BackupExeName);
            if (!File.Exists(realExePath) || HasActiveRuntime(gameFolderPath))
                return;

            TryForceDelete(realExePath);
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

        /// <summary>
        /// Subfolders kept across UI uninstall (backup for safety, logs for diagnostics).
        /// The manager UI is Distribution-only — do not keep Asher.App.
        /// </summary>
        private static bool ShouldPreserveAsherSubfolder(string directoryName) =>
            directoryName.Equals(BackupFolderName, StringComparison.OrdinalIgnoreCase)
            || directoryName.Equals(LogsFolderName, StringComparison.OrdinalIgnoreCase);

        private static void CleanRuntimeFiles(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return;

            foreach (var file in Directory.GetFiles(asherFolder))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Best effort — locked files must not abort uninstall.
                }
            }

            foreach (var directory in Directory.GetDirectories(asherFolder))
            {
                var directoryName = Path.GetFileName(directory);
                if (ShouldPreserveAsherSubfolder(directoryName))
                    continue;

                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {
                    // Best effort — e.g. file locks under Mods/.
                }
            }

            // Legacy WPF layout (game/Asher.App).
            var legacyManagerPath = Path.Combine(gameFolderPath, AsherPaths.ManagerFolderName);
            if (Directory.Exists(legacyManagerPath))
            {
                try
                {
                    Directory.Delete(legacyManagerPath, true);
                }
                catch
                {
                    // Ignore — may be locked or absent after migration.
                }
            }

            // Emergency helpers live in the game folder root (outside Asher/).
            TryDeleteFile(AsherPaths.GetEmergencyUninstallCmdPath(gameFolderPath));
            TryDeleteFile(AsherPaths.GetEmergencyUninstallPowerShellPath(gameFolderPath));
        }

        private static string GetAsherInstallationPath() =>
            AppDomain.CurrentDomain.BaseDirectory;

        private string ResolveInstallSourceFolder(string gamePath)
        {
            foreach (var candidate in GetInstallSourceCandidates(gamePath))
            {
                if (HasRequiredRuntimeFiles(candidate))
                    return candidate;
            }

            throw new FileNotFoundException(
                "Arquivos de instalação do Asher não encontrados. " +
                "Reinstale a partir do Asher Host (Electron) ou verifique install-payload/ ao lado de Asher.Host.exe.");
        }

        private IEnumerable<string> GetInstallSourceCandidates(string gamePath)
        {
            yield return GetAsherInstallationPath();
            yield return Path.Combine(GetAsherInstallationPath(), AsherPaths.HostInstallPayloadFolderName);
            yield return AsherPaths.GetRuntimeFolderPath(gamePath);
        }

        private static bool HasRequiredRuntimeFiles(string folder) =>
            RequiredRuntimeFiles.All(fileName => File.Exists(Path.Combine(folder, fileName)));

        #endregion
    }
}
