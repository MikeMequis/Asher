using Asher.Core;
using Asher.Core.Platform;

namespace Asher.Services.Tests
{
    public class AsherPathsTests
    {
        [Fact]
        public void Detects_installed_layout_using_injected_platform()
        {
            using var temp = new TempDirectory();
            var game = temp.CreateGameFolder("game");
            temp.WriteFile(Path.Combine("game", "Asher", "libasher_bootstrap.so"), string.Empty);
            temp.WriteFile(Path.Combine("game", "Asher", "Asher.Runtime.dll"), string.Empty);

            Assert.True(AsherPaths.IsAsherInstalledIn(game, new LinuxPlatformInfo()));
            Assert.False(AsherPaths.IsAsherInstalledIn(game, new WindowsPlatformInfo()));
        }

        [Fact]
        public void Uses_canonical_log_folder_name()
        {
            Assert.Equal("AsherLogs", AsherPaths.LogsFolderName);
            Assert.Equal(
                Path.Combine("game", "Asher", "AsherLogs"),
                AsherPaths.GetLogsFolderPath("game"));
        }

        [Fact]
        public void Game_relative_paths_nest_under_runtime_folder()
        {
            var game = Path.Combine("root", "game");
            var asher = Path.Combine(game, "Asher");

            Assert.Equal(asher, AsherPaths.GetRuntimeFolderPath(game));
            Assert.Equal(Path.Combine(asher, "Mods"), AsherPaths.GetModsFolderPath(game));
            Assert.Equal(Path.Combine(asher, "Mods", "disabled"), AsherPaths.GetDisabledModsFolderPath(game));
            Assert.Equal(Path.Combine(asher, "Asher.Backup"), AsherPaths.GetBackupFolderPath(game));
            Assert.Equal(Path.Combine(asher, "patches"), AsherPaths.GetPatchesFolderPath(game));
        }

        [Fact]
        public void Settings_file_name_is_stable()
        {
            Assert.Equal("settings.json", AsherPaths.SettingsFileName);
        }
    }
}
