namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Platform-specific game folder discovery. Implementations know the OS/store layout;
    /// <see cref="Implementations.GameFolderService"/> keeps the shared validation/selection order.
    /// </summary>
    public interface IGameFolderDiscovery
    {
        IEnumerable<GameFolderCandidate> EnumerateGameFolderCandidates();
    }
}
