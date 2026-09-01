using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Application.Contracts;

namespace Asher.Services.Application
{
    internal static class ApplicationContractMapper
    {
        public static ApplicationSettingsDto ToDto(AsherSettings settings) => new()
        {
            GameFolderPath = settings.GameFolderPath,
            IsInstalled = settings.IsInstalled,
            InstallationDate = settings.InstallationDate,
            GameVersion = settings.GameVersion,
            FirstRun = settings.FirstRun,
            Language = settings.Language,
            AutoLaunchEnabled = settings.AutoLaunchEnabled,
            BackupEnabled = settings.BackupEnabled,
            Theme = settings.Theme,
            CheckForUpdatesEnabled = settings.CheckForUpdatesEnabled
        };

        public static AsherSettings ToCore(ApplicationSettingsDto dto) => new()
        {
            GameFolderPath = dto.GameFolderPath,
            IsInstalled = dto.IsInstalled,
            InstallationDate = dto.InstallationDate,
            GameVersion = dto.GameVersion,
            FirstRun = dto.FirstRun,
            Language = dto.Language,
            AutoLaunchEnabled = dto.AutoLaunchEnabled,
            BackupEnabled = dto.BackupEnabled,
            Theme = dto.Theme,
            CheckForUpdatesEnabled = dto.CheckForUpdatesEnabled
        };

        public static GameFolderDto ToDto(GameFolderInfo info) => new()
        {
            Path = info.Path ?? string.Empty,
            Version = info.Version ?? string.Empty,
            IsValid = info.IsValid,
            Source = info.Source ?? string.Empty
        };

        public static GameFolderInfo ToCore(GameFolderDto dto) => new()
        {
            Path = dto.Path,
            Version = dto.Version,
            IsValid = dto.IsValid,
            Source = dto.Source
        };

        public static ManagedModDto ToDto(ManagedModInfo mod) => new()
        {
            FileName = mod.FileName,
            Name = mod.Name,
            Description = mod.Description,
            IsEnabled = mod.IsEnabled
        };

        public static InstallationResultDto ToDto(InstallationResult result) => new()
        {
            Success = result.Success,
            Message = result.Message,
            GameFolderPath = result.GameFolderPath,
            ErrorMessage = result.Error?.Message
        };

        public static InstallationProgressDto ToDto(InstallationProgress progress) => new()
        {
            Percentage = progress.Percentage,
            Message = progress.Message ?? string.Empty,
            Details = progress.Details ?? string.Empty
        };
    }
}
