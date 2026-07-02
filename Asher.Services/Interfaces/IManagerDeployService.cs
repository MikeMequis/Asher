namespace Asher.Services.Interfaces
{
    public interface IManagerDeployService
    {
        string GetPayloadFolderPath(string gameFolderPath);
        bool ShouldDeferDeploy(string gameFolderPath);
        bool HasPendingPayload(string gameFolderPath);
        void StagePayload(string sourceFolder, string gameFolderPath);
        void DeployImmediate(string sourceFolder, string gameFolderPath);
        void ApplyPendingPayload(string gameFolderPath);
        bool IsRunningFromManagerOf(string gameFolderPath);
    }
}
