using System;

namespace Asher.Runtime.Core
{
    public sealed class RuntimeContext
    {
        public string GamePath { get; }
        public string ModsPath { get; }
        public string ProfileName { get; }
        public string LogPath { get; }

        public RuntimeContext(
            string gamePath,
            string modsPath,
            string profileName,
            string logPath)
        {
            GamePath = gamePath ?? throw new ArgumentNullException(nameof(gamePath));
            ModsPath = modsPath ?? throw new ArgumentNullException(nameof(modsPath));
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? "default" : profileName;
            LogPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
        }
    }
}
