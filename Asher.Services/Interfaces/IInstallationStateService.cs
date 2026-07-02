using Asher.Core.Models;

namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Serviço para gerenciar o estado da instalação
    /// </summary>
    public interface IInstallationStateService
    {
        void SetGameFolder(GameFolderInfo gameFolder);
        GameFolderInfo GetGameFolder();
        void SetInstallationResult(InstallationResult result);
        InstallationResult GetInstallationResult();
        bool IsInstalled();
        void ClearState();
    }
}
