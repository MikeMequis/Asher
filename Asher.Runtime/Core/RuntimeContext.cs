using System;

namespace Asher.Runtime.Core
{
    public sealed class RuntimeContext
    {
        public string GamePath { get; }
        public string ModsPath { get; }
        public string ProfileName { get; }
        public string LogPath { get; }

        public RuntimeContext(string gamePath, string modsPath, string profileName, string logPath)
        {
            GamePath = gamePath ?? throw new ArgumentNullException(nameof(gamePath));
            ModsPath = modsPath ?? throw new ArgumentNullException(nameof(modsPath));
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? "default" : profileName;
            LogPath = logPath ?? throw new ArgumentNullException(nameof(logPath));

            if (string.IsNullOrWhiteSpace(GamePath))
                throw new ArgumentException("GamePath cannot be empty or whitespace", nameof(gamePath));

            if (string.IsNullOrWhiteSpace(ModsPath))
                throw new ArgumentException("ModsPath cannot be empty or whitespace", nameof(modsPath));
        }

        public override string ToString() => $"RuntimeContext[Profile={ProfileName}, Game={GamePath}]";
    }
}