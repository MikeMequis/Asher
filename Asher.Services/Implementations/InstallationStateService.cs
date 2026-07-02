using Asher.Core.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class InstallationStateService : IInstallationStateService
    {
        private GameFolderInfo _gameFolder;
        private InstallationResult _installationResult;

        public void SetGameFolder(GameFolderInfo gameFolder)
        {
            _gameFolder = gameFolder;
        }

        public GameFolderInfo GetGameFolder()
        {
            return _gameFolder;
        }

        public void SetInstallationResult(InstallationResult result)
        {
            _installationResult = result;
        }

        public InstallationResult GetInstallationResult()
        {
            return _installationResult;
        }

        public bool IsInstalled()
        {
            return _installationResult?.Success ?? false;
        }

        public void ClearState()
        {
            _gameFolder = null;
            _installationResult = null;
        }
    }
}
