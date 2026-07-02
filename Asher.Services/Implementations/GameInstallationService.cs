using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;
using System.Reflection;

namespace Asher.Services.Implementations
{
    public class GameInstallationService : IGameInstallationService
    {
        private const string OriginalExeName = AsherPaths.GameExecutableName;
        private const string BackupExeName = AsherPaths.RealGameExecutableName;
        private const string LauncherExeName = AsherPaths.LauncherExecutableName;
        private const string BackupFolderName = AsherPaths.BackupFolderName;
        private const string AsherFolderName = AsherPaths.RuntimeFolderName;
        private const string ManagerFolderName = AsherPaths.ManagerFolderName;
        private const string ModsFolderName = "Mods";
        private const string LogsFolderName = "AsherLogs";

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
            "Asher.Patching.GraphicsDeprofiler.dll"
        };

        private static readonly string[] ManagerCopyExcludedFiles =
        {
            AsherPaths.SettingsFileName,
            AsherPaths.GameExecutableName,
            AsherPaths.RealGameExecutableName,
            AsherPaths.LauncherExecutableName,
            "Asher.Runtime.dll",
            "Asher.SDK.dll",
            "0Harmony.dll"
        };

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

                await Task.Run(() => AsherPaths.MigrateLegacyLayout(gamePath));

                // Verifica se já está instalado
                if (IsInstalled(gamePath))
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "O Asher já está instalado neste jogo"
                    };
                }

                // Passo 1: Criar backup (20%)
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

                // Passo 2: Criar estrutura de pastas (30%)
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

                // Passo 3: Copiar arquivos do runtime (50%)
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

                // Passo 4: Copiar mods padrão (65%)
                progress?.Report(new InstallationProgress
                {
                    Percentage = 55,
                    Message = "Instalando mods padrão...",
                    Details = "Copiando DebugEnabler, IntroSkipper, GraphicsDeprofiler"
                });

                await Task.Run(() => CopyDefaultMods(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 65,
                    Message = "Mods padrão instalados",
                    Details = "3 mods básicos instalados"
                });

                // Passo 5: Renomear executável original (75%)
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

                // Passo 6: Copiar launcher (90%)
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

                // Passo 7: Verificação final (100%)
                progress?.Report(new InstallationProgress
                {
                    Percentage = 95,
                    Message = "Verificando instalação...",
                    Details = "Validando arquivos instalados"
                });

                await Task.Run(() => VerifyInstallation(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 92,
                    Message = "Instalando Asher App...",
                    Details = $"Copiando gerenciador para {ManagerFolderName}/"
                });

                await Task.Run(() => DeployManagerApp(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 100,
                    Message = "Instalação concluída!",
                    Details = "O Asher está pronto para uso"
                });

                return new InstallationResult
                {
                    Success = true,
                    Message = "Instalação concluída com sucesso!",
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
            var backupExePath = Path.Combine(gameFolderPath, BackupExeName);
            var asherFolder = Path.Combine(gameFolderPath, AsherFolderName);

            // Considera instalado se existe o DustAET.real.exe E a pasta Asher
            return File.Exists(backupExePath) && Directory.Exists(asherFolder);
        }

        #region Private Methods

        private void CreateBackup(string gamePath, string originalExePath)
        {
            var backupFolder = AsherPaths.GetBackupFolderPath(gamePath);

            if (!Directory.Exists(backupFolder))
                Directory.CreateDirectory(backupFolder);

            var backupExePath = Path.Combine(backupFolder, OriginalExeName);

            // Copia o executável original para o backup
            File.Copy(originalExePath, backupExePath, overwrite: true);

            // Cria arquivo de metadata do backup
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
            Directory.CreateDirectory(AsherPaths.GetPatchesFolderPath(gamePath));
            Directory.CreateDirectory(AsherPaths.GetBackupFolderPath(gamePath));
            Directory.CreateDirectory(AsherPaths.GetDisabledModsFolderPath(gamePath));
            Directory.CreateDirectory(Path.Combine(AsherPaths.GetModsFolderPath(gamePath), "config"));
            Directory.CreateDirectory(Path.Combine(AsherPaths.GetModsFolderPath(gamePath), "cache"));
        }

        private void CopyRuntimeFiles(string gamePath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            var sourceFolder = GetAsherInstallationPath();

            foreach (var fileName in RequiredRuntimeFiles)
            {
                var sourcePath = Path.Combine(sourceFolder, fileName);
                var destPath = Path.Combine(asherFolder, fileName);

                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException($"Arquivo necessário não encontrado: {fileName}");

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
                            "Use a versão net472 de packages\\Lib.Harmony.2.4.2\\lib\\net472 e execute PrepareDistribution.ps1 novamente.");
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

        private void CopyDefaultMods(string gamePath)
        {
            var modsFolder = AsherPaths.GetModsFolderPath(gamePath);
            var sourceFolder = Path.Combine(GetAsherInstallationPath(), AsherPaths.DefaultModsFolderName);

            // Se não houver pasta de mods padrão, apenas continua
            if (!Directory.Exists(sourceFolder))
                return;

            foreach (var fileName in DefaultModFiles)
            {
                var sourcePath = Path.Combine(sourceFolder, fileName);
                var destPath = Path.Combine(modsFolder, fileName);

                if (File.Exists(sourcePath))
                    File.Copy(sourcePath, destPath, overwrite: true);
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
            var sourceFolder = GetAsherInstallationPath();
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
            // Verifica se DustAET.exe existe (novo launcher)
            var launcherPath = Path.Combine(gamePath, OriginalExeName);
            if (!File.Exists(launcherPath))
                throw new InvalidOperationException("Launcher não foi instalado corretamente");

            // Verifica se DustAET.real.exe existe
            var backupExePath = Path.Combine(gamePath, BackupExeName);
            if (!File.Exists(backupExePath))
                throw new InvalidOperationException("Executável original não foi renomeado");

            // Verifica se pasta Asher existe
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            if (!Directory.Exists(asherFolder))
                throw new InvalidOperationException("Pasta Asher não foi criada");

            // Verifica se arquivos do runtime existem
            foreach (var fileName in RequiredRuntimeFiles)
            {
                var filePath = Path.Combine(asherFolder, fileName);
                if (!File.Exists(filePath))
                    throw new InvalidOperationException($"Arquivo do runtime não encontrado: {fileName}");
            }
        }

        private void DeployManagerApp(string gamePath)
        {
            var sourceFolder = NormalizeDirectoryPath(GetAsherInstallationPath());
            var destinationFolder = NormalizeDirectoryPath(AsherPaths.GetManagerFolderPath(gamePath));

            if (string.Equals(sourceFolder, destinationFolder, StringComparison.OrdinalIgnoreCase))
                return;

            Directory.CreateDirectory(destinationFolder);
            CopyManagerDirectory(sourceFolder, destinationFolder);
        }

        private static string NormalizeDirectoryPath(string path) =>
            Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

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

        private static void CleanRuntimeFiles(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return;

            foreach (var file in Directory.GetFiles(asherFolder))
                File.Delete(file);

            foreach (var directory in Directory.GetDirectories(asherFolder))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName.Equals(ManagerFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(BackupFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Directory.Delete(directory, true);
            }

            var legacyManagerPath = Path.Combine(gameFolderPath, ManagerFolderName);
            var installedManagerPath = AsherPaths.GetManagerFolderPath(gameFolderPath);
            if (Directory.Exists(legacyManagerPath)
                && !string.Equals(
                    Path.GetFullPath(legacyManagerPath),
                    Path.GetFullPath(installedManagerPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(legacyManagerPath, true);
            }
        }

        private static void CopyManagerDirectory(string sourceFolder, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                var fileName = Path.GetFileName(file);
                if (ShouldSkipManagerFile(fileName))
                    continue;

                var destPath = Path.Combine(destinationFolder, fileName);
                if (string.Equals(
                    Path.GetFullPath(file),
                    Path.GetFullPath(destPath),
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TryCopyManagerFile(file, destPath);
            }

            foreach (var directory in Directory.GetDirectories(sourceFolder))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName.Equals(AsherPaths.RuntimeFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(AsherPaths.BackupFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(ManagerFolderName, StringComparison.OrdinalIgnoreCase)
                    || directoryName.Equals(AsherPaths.DefaultModsFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetDirectory = Path.Combine(destinationFolder, directoryName);
                Directory.CreateDirectory(targetDirectory);
                CopyManagerDirectory(directory, targetDirectory);
            }
        }

        private static bool ShouldSkipManagerFile(string fileName) =>
            ManagerCopyExcludedFiles.Any(excluded =>
                fileName.Equals(excluded, StringComparison.OrdinalIgnoreCase));

        private static void TryCopyManagerFile(string sourcePath, string destPath)
        {
            try
            {
                File.Copy(sourcePath, destPath, overwrite: true);
            }
            catch (IOException) when (File.Exists(destPath))
            {
                // The manager may be running from the destination folder during reinstall.
            }
            catch (UnauthorizedAccessException) when (File.Exists(destPath))
            {
            }
        }

        private string GetAsherInstallationPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        #endregion
    }
}
