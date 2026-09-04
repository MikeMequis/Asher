using Asher.Core;
using Asher.Core.Models;
using Asher.Services;
using Asher.Services.Application.Contracts;
using Asher.Services.Hosting;

namespace Asher.Services.Application
{
    public sealed class AsherApplication : IAsherApplication
    {
        private static readonly IProgress<InstallationProgress> NoProgress =
            new Progress<InstallationProgress>(_ => { });

        private readonly ApplicationServices _services;

        public AsherApplication(ApplicationServices services)
        {
            _services = services;
        }

        public ApplicationSettingsDto GetSettings() =>
            ApplicationContractMapper.ToDto(_services.Settings.Load());

        public void SaveSettings(ApplicationSettingsDto settings) =>
            _services.Settings.Save(ApplicationContractMapper.ToCore(settings));

        public ApplicationMode GetApplicationMode()
        {
            var settings = _services.Settings.Load();
            var candidate = settings.GameFolderPath;
            var installed = !string.IsNullOrWhiteSpace(candidate)
                            && _services.Installation.IsInstalled(candidate);
            var mode = installed ? ApplicationMode.Manager : ApplicationMode.InstallWizard;

            InstallFlowTrace.Log(
                "GetApplicationMode",
                $"path={candidate ?? "(none)"} settings.IsInstalled={settings.IsInstalled} diskInstalled={installed} => {mode}");

            return mode;
        }

        public GameFolderDto DetectGameFolder() =>
            ApplicationContractMapper.ToDto(_services.GameFolders.DetectGameFolder());

        public GameFolderDto GetGameFolderInfo(string folderPath) =>
            ApplicationContractMapper.ToDto(_services.GameFolders.GetInfo(folderPath));

        public string? ResolveGameFolderPath() => _services.Launch.ResolveGameFolderPath();

        public bool IsGameInstalled(string? gameFolderPath = null)
        {
            var path = gameFolderPath ?? ResolveGameFolderPath();
            var installed = !string.IsNullOrWhiteSpace(path) && _services.Installation.IsInstalled(path);
            var markers = !string.IsNullOrWhiteSpace(path)
                ? _services.Installation.DescribeInstallState(path)
                : "(no path)";

            InstallFlowTrace.Log("IsGameInstalled", $"path={path ?? "(none)"} => {installed} | {markers}");

            return installed;
        }

        public bool HasRestorableBackup(string? gameFolderPath = null)
        {
            var path = gameFolderPath ?? ResolveGameFolderPath();
            return !string.IsNullOrWhiteSpace(path)
                   && _services.Installation.HasRestorableBackup(path);
        }

        public string DescribeInstallState(string? gameFolderPath = null)
        {
            var path = gameFolderPath ?? ResolveGameFolderPath();
            return string.IsNullOrWhiteSpace(path)
                ? "Nenhum caminho de jogo configurado."
                : _services.Installation.DescribeInstallState(path);
        }

        public async Task<IReadOnlyList<ManagedModDto>> GetModsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mods = await _services.Patches.GetModsAsync();
            return mods.Select(ApplicationContractMapper.ToDto).ToList();
        }

        public async Task<OperationResult> SetModEnabledAsync(
            string modFileName,
            bool enabled,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _services.Patches.SetModEnabledAsync(modFileName, enabled))
                return OperationResult.Succeeded();

            return OperationResult.Failed($"Failed to set mod '{modFileName}' enabled={enabled}.");
        }

        public async Task<InstallationResultDto> InstallAsync(
            GameFolderDto gameInfo,
            IProgress<InstallationProgressDto>? progress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _services.Installation.InstallAsync(
                ApplicationContractMapper.ToCore(gameInfo),
                CreateProgress(progress));

            return ApplicationContractMapper.ToDto(result);
        }

        public async Task<InstallationResultDto> UninstallAsync(
            string? gameFolderPath = null,
            IProgress<InstallationProgressDto>? progress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = gameFolderPath ?? ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return new InstallationResultDto
                {
                    Success = false,
                    Message = "No game folder path is configured."
                };
            }

            var result = await _services.Installation.UninstallAsync(path, CreateProgress(progress));
            return ApplicationContractMapper.ToDto(result);
        }

        public OperationResult LaunchGame()
        {
            if (_services.Launch.TryLaunchGame(out var errorMessage))
                return OperationResult.Succeeded();

            return OperationResult.Failed(errorMessage);
        }

        public void MarkInstalled(string gameFolderPath, string gameVersion)
        {
            InstallFlowTrace.Log("MarkInstalled", $"path={gameFolderPath} version={gameVersion}");
            _services.Settings.MarkAsInstalled(gameFolderPath, gameVersion);
        }

        public void MarkUninstalled()
        {
            InstallFlowTrace.Log("MarkUninstalled", "settings flag cleared");
            _services.Settings.MarkAsUninstalled();
        }

        private static IProgress<InstallationProgress> CreateProgress(IProgress<InstallationProgressDto>? progress)
        {
            if (progress == null)
                return NoProgress;

            return new Progress<InstallationProgress>(value =>
                progress.Report(ApplicationContractMapper.ToDto(value)));
        }
    }
}
