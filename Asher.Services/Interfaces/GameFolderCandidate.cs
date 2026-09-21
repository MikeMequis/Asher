namespace Asher.Services.Interfaces
{
    /// <summary>
    /// A folder that may contain the game, plus a human-readable discovery source
    /// (e.g. "Steam", "GOG", "Settings").
    /// </summary>
    public readonly struct GameFolderCandidate
    {
        public GameFolderCandidate(string path, string source)
        {
            Path = path;
            Source = source;
        }

        public string Path { get; }

        public string Source { get; }
    }
}
