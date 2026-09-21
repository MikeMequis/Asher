namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Platform-specific process start for the game executable.
    /// Windows starts the swapped launcher directly; Linux must export the bootstrap
    /// environment (LD_PRELOAD, ASHER_*, MONO_PATH) before starting the native DustAET.
    /// </summary>
    public interface IGameProcessLauncher
    {
        bool TryStart(string executablePath, string workingDirectory, out string? errorMessage);
    }
}
