using Asher.Core.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class GameInstallationService : IGameInstallationService
    {
        private const string OriginalExeName = "DustAET.exe";
        private const string BackupExeName = "DustAET.real.exe";
        private const string LauncherExeName = "Asher.Launcher.exe";
        private const string BackupFolderName = "Asher.Backup";
        private const string AsherFolderName = "Asher";
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
                    Percentage = 100,
                    Message = "Instalação concluída!",
                    Details = "O Asher está pronto para uso"
                });

                return new InstallationResult
                {
                    Success = true,
                    Message = "Instalação concluída com sucesso!"
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

                // Passo 1: Remover launcher
                progress?.Report(new InstallationProgress
                {
                    Percentage = 20,
                    Message = "Removendo Asher Launcher...",
                    Details = "Deletando DustAET.exe modificado"
                });

                await Task.Run(() =>
                {
                    var launcherPath = Path.Combine(gameFolderPath, OriginalExeName);
                    if (File.Exists(launcherPath))
                        File.Delete(launcherPath);

                    var launcherConfigPath = launcherPath + ".config";
                    if (File.Exists(launcherConfigPath))
                        File.Delete(launcherConfigPath);
                });

                // Passo 2: Restaurar executável original
                progress?.Report(new InstallationProgress
                {
                    Percentage = 40,
                    Message = "Restaurando executável original...",
                    Details = "DustAET.real.exe → DustAET.exe"
                });

                await Task.Run(() =>
                {
                    var backupExe = Path.Combine(gameFolderPath, BackupExeName);
                    var originalExe = Path.Combine(gameFolderPath, OriginalExeName);

                    if (File.Exists(backupExe))
                        File.Move(backupExe, originalExe);
                });

                // Passo 3: Remover pasta Asher (opcional - manter logs?)
                progress?.Report(new InstallationProgress
                {
                    Percentage = 70,
                    Message = "Limpando arquivos do Asher...",
                    Details = "Removendo pasta Asher/"
                });

                await Task.Run(() =>
                {
                    var asherFolder = Path.Combine(gameFolderPath, AsherFolderName);
                    if (Directory.Exists(asherFolder))
                        Directory.Delete(asherFolder, true);
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 100,
                    Message = "Desinstalação concluída",
                    Details = "Jogo restaurado ao estado original"
                });

                return new InstallationResult
                {
                    Success = true,
                    Message = "Asher desinstalado com sucesso"
                };
            }
            catch (Exception ex)
            {
                return new InstallationResult
                {
                    Success = false,
                    Message = $"Erro durante desinstalação: {ex.Message}",
                    Error = ex
                };
            }
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
            var backupFolder = Path.Combine(gamePath, BackupFolderName);

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
            // Criar pasta principal Asher/
            var asherFolder = Path.Combine(gamePath, AsherFolderName);
            if (!Directory.Exists(asherFolder))
                Directory.CreateDirectory(asherFolder);

            // Criar subpastas dentro de Asher/
            var modsFolder = Path.Combine(asherFolder, ModsFolderName);
            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);

            var logsFolder = Path.Combine(asherFolder, LogsFolderName);
            if (!Directory.Exists(logsFolder))
                Directory.CreateDirectory(logsFolder);

            // Criar subpastas de Mods/
            var configFolder = Path.Combine(modsFolder, "config");
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            var cacheFolder = Path.Combine(modsFolder, "cache");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);
        }

        private void CopyRuntimeFiles(string gamePath)
        {
            var asherFolder = Path.Combine(gamePath, AsherFolderName);
            var sourceFolder = GetAsherInstallationPath();

            foreach (var fileName in RequiredRuntimeFiles)
            {
                var sourcePath = Path.Combine(sourceFolder, fileName);
                var destPath = Path.Combine(asherFolder, fileName);

                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException($"Arquivo necessário não encontrado: {fileName}");

                File.Copy(sourcePath, destPath, overwrite: true);
            }
        }

        private void CopyDefaultMods(string gamePath)
        {
            var modsFolder = Path.Combine(gamePath, AsherFolderName, ModsFolderName);
            var sourceFolder = Path.Combine(GetAsherInstallationPath(), "DefaultMods");

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
            var asherFolder = Path.Combine(gamePath, AsherFolderName);
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

        private string GetAsherInstallationPath()
        {
            // Retorna o diretório onde o Asher.UserInterface.exe está rodando
            // Aqui devem estar os arquivos para copiar: Asher.Launcher.exe, DLLs, etc.
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        #endregion
    }
}
