namespace Asher.Services.Application.Contracts
{
    public sealed class ApplicationSettingsDto
    {
        public string GameFolderPath { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public DateTime? InstallationDate { get; set; }
        public string GameVersion { get; set; } = string.Empty;
        public bool FirstRun { get; set; } = true;
        public string Language { get; set; } = "en-US";
        public bool AutoLaunchEnabled { get; set; } = true;
        public bool BackupEnabled { get; set; } = true;
        public string Theme { get; set; } = "Light";
        public bool CheckForUpdatesEnabled { get; set; } = true;
    }
}
