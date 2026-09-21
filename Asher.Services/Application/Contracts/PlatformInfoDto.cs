namespace Asher.Services.Application.Contracts
{
    /// <summary>
    /// Platform description surfaced to the frontend so it can adapt launch/removal UI
    /// without embedding OS knowledge in JavaScript.
    /// </summary>
    public sealed class PlatformInfoDto
    {
        public string Kind { get; set; } = string.Empty;
        public string GameExecutableName { get; set; } = string.Empty;
        public string RealGameExecutableName { get; set; } = string.Empty;
        public string LauncherExecutableName { get; set; } = string.Empty;
        public string BootstrapLibraryName { get; set; } = string.Empty;
        public bool UsesLauncherSwap { get; set; }
        public bool SupportsRecoveryHelper { get; set; }
        public string DefaultGameFolderName { get; set; } = string.Empty;
    }
}
