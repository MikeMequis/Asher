using Asher.Services.Application.Contracts;

namespace Asher.Services.Application
{
    /// <summary>
    /// In-process application boundary for frontends. Delegates to internal services.
    /// </summary>
    public interface IAsherApplication
    {
        ApplicationSettingsDto GetSettings();
        void SaveSettings(ApplicationSettingsDto settings);

        ApplicationMode GetApplicationMode();

        GameFolderDto DetectGameFolder();
        GameFolderDto GetGameFolderInfo(string folderPath);
        string? ResolveGameFolderPath();
        bool IsGameInstalled(string? gameFolderPath = null);
        bool HasRestorableBackup(string? gameFolderPath = null);

        Task<IReadOnlyList<ManagedModDto>> GetModsAsync(CancellationToken cancellationToken = default);
        Task<OperationResult> SetModEnabledAsync(
            string modFileName,
            bool enabled,
            CancellationToken cancellationToken = default);

        Task<InstallationResultDto> InstallAsync(
            GameFolderDto gameInfo,
            IProgress<InstallationProgressDto>? progress = null,
            CancellationToken cancellationToken = default);

        Task<InstallationResultDto> UninstallAsync(
            string? gameFolderPath = null,
            IProgress<InstallationProgressDto>? progress = null,
            CancellationToken cancellationToken = default);

        OperationResult LaunchGame();
    }
}
