using Asher.Core;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class GameLaunchService : IGameLaunchService
    {
        private readonly IGameFolderService _gameFolderService;
        private readonly IGameInstallationService _installationService;
        private readonly IGameProcessLauncher _processLauncher;

        public GameLaunchService(
            IGameFolderService gameFolderService,
            IGameInstallationService installationService,
            IGameProcessLauncher processLauncher)
        {
            _gameFolderService = gameFolderService;
            _installationService = installationService;
            _processLauncher = processLauncher;
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

            return _processLauncher.TryStart(executablePath, gameFolder, out errorMessage);
        }
    }
}
