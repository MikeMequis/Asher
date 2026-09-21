using Asher.Core.Platform;

namespace Asher.Services.Tests
{
    public class WindowsPlatformInfoTests
    {
        [Fact]
        public void Describes_launcher_swap_layout()
        {
            var info = new WindowsPlatformInfo();

            Assert.Equal(PlatformKind.Windows, info.Kind);
            Assert.Equal("DustAET.exe", info.GameExecutableName);
            Assert.Equal("DustAET.real.exe", info.RealGameExecutableName);
            Assert.Equal("Asher.Launcher.exe", info.LauncherExecutableName);
            Assert.Equal(string.Empty, info.BootstrapLibraryName);
            Assert.True(info.UsesLauncherSwap);
            Assert.True(info.SupportsRecoveryHelper);
            Assert.True(info.WritesPortableSettings);
        }

        [Fact]
        public void Settings_directory_is_under_roaming_appdata()
        {
            var expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Asher");

            Assert.Equal(expected, new WindowsPlatformInfo().UserSettingsDirectory);
        }
    }
}
