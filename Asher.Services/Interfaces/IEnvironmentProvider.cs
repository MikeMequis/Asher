namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Snapshot of the parent process environment, preserved when launching a child process.
    /// </summary>
    public interface IEnvironmentProvider
    {
        IReadOnlyDictionary<string, string> GetEnvironmentVariables();
    }
}
