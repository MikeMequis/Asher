using Asher.Core;
using Asher.Core.Models;
using Asher.Core.Platform;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    /// <summary>
    /// Platform-independent install/uninstall orchestration: validation, progress reporting,
    /// folder structure, payload discovery and verification. All OS-specific work is delegated
    /// to <see cref="IGameExecutableLayout"/> and <see cref="IRuntimeDeployment"/>, so no
    /// separate Linux installer is needed.
    /// </summary>
    public class GameInstallationService : IGameInstallationService
    {
        private static string OriginalExeName => AsherPaths.GameExecutableName;
        private static string BackupExeName => AsherPaths.RealGameExecutableName;

        private readonly IGameExecutableLayout _layout;
        private readonly IRuntimeDeployment _deployment;
        private readonly IPlatformInfo _platform;

        public GameInstallationService(
            IGameExecutableLayout layout,
            IRuntimeDeployment deployment,
            IPlatformInfo platform)
        {
            _layout = layout;
            _deployment = deployment;
            _platform = platform;
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

                await Task.Run(() => _layout.CreateBackup(gamePath, originalExePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 20,
                    Message = "Backup criado com sucesso",
                    Details = $"Backup salvo em {AsherPaths.BackupFolderName}/"
                });

                progress?.Report(new InstallationProgress
                {
                    Percentage = 25,
                    Message = "Criando estrutura de pastas...",
                    Details = "Configurando diretórios do Asher"
                });

                await Task.Run(() => CreateFolderStructure(gamePath));
                await Task.Run(() => WriteReadme(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 30,
                    Message = "Estrutura de pastas criada",
                    Details = "Pastas Asher/, Mods/ e AsherLogs/ criadas"
                });

                await Task.Run(() => _layout.InstallRecoveryHelper(gamePath));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 35,
                    Message = "Copiando arquivos do runtime...",
                    Details = "Instalando Asher.Runtime.dll, Asher.SDK.dll, 0Harmony.dll"
                });

                var installSourceFolder = await Task.Run(() => ResolveInstallSourceFolder(gamePath));
                await Task.Run(() => _deployment.DeployRuntimeFiles(gamePath, installSourceFolder));

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

                var modsCopied = await Task.Run(
                    () => _deployment.DeployDefaultMods(gamePath, GetDefaultModsSourceCandidates(gamePath)));

                progress?.Report(new InstallationProgress
                {
                    Percentage = 65,
                    Message = modsCopied > 0 ? "Mods padrão instalados" : "Nenhum mod padrão no payload",
                    Details = modsCopied > 0
                        ? $"{modsCopied} mod(s) copiado(s) para Asher/Mods/"
                        : "Payload sem DefaultMods — patches não carregarão até mods serem adicionados"
                });

                if (_platform.UsesLauncherSwap)
                {
                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 70,
                        Message = "Configurando executáveis...",
                        Details = $"Renomeando {OriginalExeName} → {BackupExeName}"
                    });

                    await Task.Run(() => _layout.RenameOriginalExecutable(gamePath));

                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 75,
                        Message = "Executável original preservado",
                        Details = $"{BackupExeName} criado"
                    });

                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 80,
                        Message = "Instalando Asher Launcher...",
                        Details = $"Copiando {AsherPaths.LauncherExecutableName} → {OriginalExeName}"
                    });

                    var launcherSource = Path.Combine(installSourceFolder, AsherPaths.LauncherExecutableName);
                    await Task.Run(() => _layout.InstallLauncher(gamePath, launcherSource));

                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 90,
                        Message = "Launcher instalado",
                        Details = $"Novo {OriginalExeName} configurado"
                    });
                }
                else
                {
                    // Linux: no executable swap — DustAET stays native and Asher attaches via LD_PRELOAD.
                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 90,
                        Message = "Bootstrap do runtime preparado",
                        Details = $"{_platform.BootstrapLibraryName} configurado para {OriginalExeName}"
                    });
                }

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

                if (_platform.UsesLauncherSwap && !_layout.HasRestorableBackup(gameFolderPath))
                {
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "Nenhum backup restaurável foi encontrado em Asher/Asher.Backup"
                    };
                }

                if (_platform.UsesLauncherSwap)
                {
                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 10,
                        Message = "Removendo Asher Launcher...",
                        Details = "Preparando restauração do executável original"
                    });

                    await Task.Run(() => _layout.RemoveLauncherFiles(gameFolderPath));

                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 35,
                        Message = "Restaurando executável original...",
                        Details = $"Restaurando {OriginalExeName} a partir do backup"
                    });

                    await Task.Run(() => _layout.RestoreOriginalExecutable(gameFolderPath));
                }
                else
                {
                    progress?.Report(new InstallationProgress
                    {
                        Percentage = 10,
                        Message = "Removendo arquivos do Asher...",
                        Details = "O executável original do jogo não é modificado"
                    });

                    await Task.Run(() => _layout.RemoveLauncherFiles(gameFolderPath));
                }

                progress?.Report(new InstallationProgress
                {
                    Percentage = 65,
                    Message = "Removendo arquivos do runtime...",
                    Details = "Limpando mods, logs e DLLs do Asher"
                });

                await Task.Run(() => _deployment.CleanRuntimeFiles(gameFolderPath));
                await Task.Run(() => _layout.RemoveRecoveryHelper(gameFolderPath));

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

        public bool HasRestorableBackup(string gameFolderPath) =>
            _layout.HasRestorableBackup(gameFolderPath);

        public bool IsInstalled(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
                return false;

            return GetInstallState(gameFolderPath).State == InstallationState.Installed;
        }

        public string DescribeInstallState(string gameFolderPath) =>
            DescribeInstallMarkers(gameFolderPath);

        public InstallStateInfo GetInstallState(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
            {
                return new InstallStateInfo
                {
                    State = InstallationState.NotInstalled,
                    CanUninstall = false,
                    CanRestore = false,
                    Marker = string.Empty
                };
            }

            var markerPresent = _layout.IsInstalled(gameFolderPath);
            var runtimePresent = _deployment.HasManagedRuntime(gameFolderPath);
            var runtimeInstalled = _deployment.IsRuntimeInstalled(gameFolderPath);
            var restorable = _layout.HasRestorableBackup(gameFolderPath);

            var state = markerPresent && runtimeInstalled
                ? InstallationState.Installed
                : markerPresent || runtimePresent
                    ? InstallationState.Partial
                    : InstallationState.NotInstalled;

            return new InstallStateInfo
            {
                State = state,
                CanUninstall = state != InstallationState.NotInstalled
                               && (!_platform.UsesLauncherSwap || restorable),
                CanRestore = _platform.UsesLauncherSwap && restorable,
                Marker = DescribeMarker(gameFolderPath, markerPresent, runtimePresent)
            };
        }

        private string DescribeMarker(string gameFolderPath, bool markerPresent, bool runtimePresent)
        {
            if (markerPresent)
            {
                return _platform.UsesLauncherSwap
                    ? _platform.RealGameExecutableName
                    : _platform.BootstrapLibraryName;
            }

            if (runtimePresent)
            {
                var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
                foreach (var fileName in _deployment.RequiredRuntimeFiles)
                {
                    if (File.Exists(Path.Combine(asherFolder, fileName)))
                        return fileName;
                }
            }

            return string.Empty;
        }

        private string DescribeInstallMarkers(string gameFolderPath)
        {
            var markers = new List<string>();

            if (_platform.UsesLauncherSwap)
            {
                var realExePath = Path.Combine(gameFolderPath, BackupExeName);
                if (File.Exists(realExePath))
                    markers.Add(BackupExeName);
            }
            else if (!string.IsNullOrEmpty(_platform.BootstrapLibraryName))
            {
                if (File.Exists(AsherPaths.GetBootstrapLibraryPath(gameFolderPath, _platform)))
                    markers.Add(_platform.BootstrapLibraryName);
            }

            var asherFolder = Path.Combine(gameFolderPath, AsherPaths.RuntimeFolderName);
            if (Directory.Exists(asherFolder))
            {
                if (HasActiveRuntime(gameFolderPath))
                    markers.Add($"pasta {AsherPaths.RuntimeFolderName}/ (runtime ativo)");
                else
                    markers.Add($"pasta {AsherPaths.RuntimeFolderName}/ (resíduo — não bloqueia reinstalação)");
            }

            if (markers.Count == 0)
                return "Nenhum marcador de instalação ativo encontrado.";

            return $"Marcadores encontrados: {string.Join(", ", markers)}";
        }

        private bool HasActiveRuntime(string gameFolderPath) =>
            _deployment.HasActiveRuntime(gameFolderPath);

        #region Private Methods

        private static void CreateFolderStructure(string gamePath)
        {
            Directory.CreateDirectory(AsherPaths.GetRuntimeFolderPath(gamePath));
            Directory.CreateDirectory(AsherPaths.GetModsFolderPath(gamePath));
            Directory.CreateDirectory(AsherPaths.GetLogsFolderPath(gamePath));
        }

        private void WriteReadme(string gamePath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            var content = _platform.UsesLauncherSwap ? WindowsReadme : LinuxReadme;
            File.WriteAllText(Path.Combine(asherFolder, "LEIA-ME.txt"), content);
        }

        private const string WindowsReadme = """
ASHER - MOD MANAGER PARA DUST: AN ELYSIAN TAIL
================================================

Arquivo gerado automaticamente na instalacao.

COMO DESINSTALAR
- Pelo gerenciador: Configuracoes > Remocao > Desinstalacao segura.
- Sem o gerenciador: execute Uninstall-Asher.cmd na pasta do jogo.

ESTRUTURA INSTALADA
DustAET.exe        Asher Launcher
DustAET.real.exe   executavel original (backup)
Asher.Backup/      backup do executavel original
Asher/             runtime, mods e logs

LOGS
Asher/AsherLogs/
""";

        private const string LinuxReadme = """
ASHER - MOD MANAGER PARA DUST: AN ELYSIAN TAIL
================================================

Arquivo gerado automaticamente na instalacao.

COMO DESINSTALAR
- Pelo gerenciador: Configuracoes > Remocao > Desinstalacao segura.
- O jogo (DustAET) nao e modificado; remover Asher/ restaura o estado original.

ESTRUTURA INSTALADA
DustAET            inalterado
Asher/
  libasher_bootstrap.so
  Asher.Runtime.dll / Asher.SDK.dll / 0Harmony.dll
  Mods/            mods ativos
  AsherLogs/       logs do runtime
  install.json     manifesto da instalacao

LOGS
Asher/AsherLogs/
""";

        private void VerifyInstallation(string gamePath)
        {
            _layout.VerifyInstalled(gamePath);

            var asherFolder = AsherPaths.GetRuntimeFolderPath(gamePath);
            if (!Directory.Exists(asherFolder))
                throw new InvalidOperationException("Pasta Asher não foi criada");

            foreach (var fileName in _deployment.RequiredRuntimeFiles)
            {
                var filePath = Path.Combine(asherFolder, fileName);
                if (!File.Exists(filePath))
                    throw new InvalidOperationException($"Arquivo do runtime não encontrado: {fileName}");
            }

            if (!_deployment.IsRuntimeInstalled(gamePath))
                throw new InvalidOperationException("O runtime do Asher não foi implantado completamente");
        }

        /// <summary>
        /// Removes leftover install markers so a subsequent install is not blocked.
        /// Keeps Asher.Backup for safety.
        /// </summary>
        private void EnsureInstallMarkersRemoved(string gameFolderPath)
        {
            _layout.RemoveInstalledMarker(gameFolderPath);
            _deployment.CleanRuntimeFiles(gameFolderPath);
        }

        private void RemoveStaleInstallMarkers(string gameFolderPath)
        {
            if (!_layout.IsInstalled(gameFolderPath) || _deployment.HasActiveRuntime(gameFolderPath))
                return;

            _layout.RemoveInstalledMarker(gameFolderPath);
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

        private IEnumerable<string> GetDefaultModsSourceCandidates(string gamePath)
        {
            foreach (var candidate in GetInstallSourceCandidates(gamePath))
            {
                foreach (var folderName in _deployment.DefaultModsSourceFolderNames)
                    yield return Path.Combine(candidate, folderName);
            }
        }

        private bool HasRequiredRuntimeFiles(string folder) =>
            _deployment.RequiredRuntimeFiles.All(fileName => File.Exists(Path.Combine(folder, fileName)));

        #endregion
    }
}
