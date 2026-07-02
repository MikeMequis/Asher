namespace Asher.Services.Interfaces
{
    public interface IGameLaunchService
    {
        string? ResolveGameFolderPath();
        bool TryLaunchGame(out string? errorMessage);
    }
}
