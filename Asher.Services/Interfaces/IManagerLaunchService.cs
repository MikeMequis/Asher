namespace Asher.Services.Interfaces
{
    public interface IManagerLaunchService
    {
        string GetInstalledManagerPath(string gameFolderPath);
        bool ShouldRelaunchAfterInstall(string gameFolderPath);
        bool TryRelaunchManager(string gameFolderPath, out string? errorMessage);
        bool TryRestartCurrentManager(out string? errorMessage);
    }
}
