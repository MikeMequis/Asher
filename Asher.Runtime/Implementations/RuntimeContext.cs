namespace Asher.Runtime.Implementations
{
    public class RuntimeContext
    {
        public string GamePath { get; }
        public string LauncherPath { get; }
        public string LogPath { get; }

        public RuntimeContext(string gamePath, string launcherPath, string logPath)
        {
            GamePath = gamePath;
            LauncherPath = launcherPath;
            LogPath = logPath;
        }
    }
}
