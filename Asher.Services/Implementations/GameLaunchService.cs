using Asher.Core;
using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Services.Implementations
{
    public class GameLaunchService : IGameLaunchService
    {
        private readonly IGameFolderService _gameFolderService;
        private readonly IGameInstallationService _installationService;

        public GameLaunchService(IGameFolderService gameFolderService, IGameInstallationService installationService)
        {
            _gameFolderService = gameFolderService;
            _installationService = installationService;
        }

        public string? ResolveGameFolderPath()
        {
            var settings = AsherSettings.Load();
            if (!string.IsNullOrWhiteSpace(settings.GameFolderPath)
                && _installationService.IsInstalled(settings.GameFolderPath))
            {
                AsherPaths.MigrateLegacyLayout(settings.GameFolderPath);
                return settings.GameFolderPath;
            }

            var detected = _gameFolderService.DetectGameFolder();
            if (detected.IsValid && _installationService.IsInstalled(detected.Path))
                return detected.Path;

            return null;
        }

        public bool TryLaunchGame(out string? errorMessage)
        {
            var gameFolder = ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
            {
                errorMessage = "Não foi possível localizar a pasta do jogo com o Asher instalado.";
                return false;
            }

            var executablePath = Path.Combine(gameFolder, AsherPaths.GameExecutableName);
            if (!File.Exists(executablePath))
            {
                errorMessage = $"Executável do jogo não encontrado: {executablePath}";
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = gameFolder,
                    UseShellExecute = true
                });

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Falha ao iniciar o jogo: {ex.Message}";
                return false;
            }
        }
    }
}
