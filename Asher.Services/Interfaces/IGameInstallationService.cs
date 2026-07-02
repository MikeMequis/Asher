using Asher.Core.Models;

namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Serviço responsável pela instalação/preparação do jogo
    /// </summary>
    public interface IGameInstallationService
    {
        Task<InstallationResult> InstallAsync(GameFolderInfo gameInfo, IProgress<InstallationProgress> progress);
        Task<InstallationResult> UninstallAsync(string gameFolderPath, IProgress<InstallationProgress> progress);
        bool IsInstalled(string gameFolderPath);
        bool HasRestorableBackup(string gameFolderPath);
    }
}
