using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class PatchManagerService : IPatchManagerService
    {
        private static readonly Dictionary<string, (string Name, string Description)> KnownMods = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Asher.Patching.DebugEnabler.dll"] = ("Debug Enabler", "Enables the debug menu in pause mode"),
            ["Asher.Patching.IntroSkipper.dll"] = ("Intro Skipper", "Skips intro sequences and splash screens"),
            ["Asher.Patching.GraphicsDeprofiler.dll"] = ("Graphics Deprofiler", "Bypasses HiDef GPU profile restrictions"),
            ["Asher.Patching.MuteVoiceActing.dll"] = ("Voice Acting Muter", "Mutes voice acting while keeping other SFX"),
            ["Asher.Patching.OverheatDisabler.dll"] = ("Dust Storm Overheat Disabler", "Prevents Dust Storm from overheating"),
        };

        private readonly IGameLaunchService _gameLaunchService;

        public PatchManagerService(IGameLaunchService gameLaunchService)
        {
            _gameLaunchService = gameLaunchService;
        }

        public Task<IReadOnlyList<ManagedModInfo>> GetModsAsync()
        {
            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult<IReadOnlyList<ManagedModInfo>>(Array.Empty<ManagedModInfo>());

            var modsFolder = AsherPaths.GetModsFolderPath(gameFolder);
            var disabledFolder = AsherPaths.GetDisabledModsFolderPath(gameFolder);
            Directory.CreateDirectory(modsFolder);
            Directory.CreateDirectory(disabledFolder);

            var mods = new List<ManagedModInfo>();

            foreach (var file in Directory.GetFiles(modsFolder, "*.dll"))
            {
                var fileName = Path.GetFileName(file);
                mods.Add(CreateModInfo(fileName, true));
            }

            foreach (var file in Directory.GetFiles(disabledFolder, "*.dll"))
            {
                var fileName = Path.GetFileName(file);
                mods.Add(CreateModInfo(fileName, false));
            }

            return Task.FromResult<IReadOnlyList<ManagedModInfo>>(mods.OrderBy(m => m.Name).ToList());
        }

        public Task<bool> SetModEnabledAsync(string modFileName, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(modFileName) ||
                !string.Equals(modFileName, Path.GetFileName(modFileName), StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult(false);

            var enabledPath = Path.Combine(AsherPaths.GetModsFolderPath(gameFolder), modFileName);
            var disabledPath = Path.Combine(AsherPaths.GetDisabledModsFolderPath(gameFolder), modFileName);

            try
            {
                var enabledExists = File.Exists(enabledPath);
                var disabledExists = File.Exists(disabledPath);

                if (!enabledExists && !disabledExists)
                    return Task.FromResult(false);

                if (enabledExists && disabledExists)
                    return Task.FromResult(false);

                if (enabled)
                {
                    if (enabledExists)
                        return Task.FromResult(true);

                    File.Move(disabledPath, enabledPath);
                }
                else
                {
                    if (disabledExists)
                        return Task.FromResult(true);

                    Directory.CreateDirectory(AsherPaths.GetDisabledModsFolderPath(gameFolder));
                    File.Move(enabledPath, disabledPath);
                }

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private static ManagedModInfo CreateModInfo(string fileName, bool isEnabled)
        {
            if (KnownMods.TryGetValue(fileName, out var metadata))
            {
                return new ManagedModInfo
                {
                    FileName = fileName,
                    Name = metadata.Name,
                    Description = metadata.Description,
                    IsEnabled = isEnabled
                };
            }

            return new ManagedModInfo
            {
                FileName = fileName,
                Name = Path.GetFileNameWithoutExtension(fileName),
                Description = "Custom mod assembly",
                IsEnabled = isEnabled
            };
        }
    }
}
