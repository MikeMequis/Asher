using Asher.Core.Platform;

namespace Asher.Services.Tests
{
    public class LinuxPlatformInfoTests
    {
        [Fact]
        public void Describes_bootstrap_layout()
        {
            var info = new LinuxPlatformInfo();

            Assert.Equal(PlatformKind.Linux, info.Kind);
            Assert.Equal("DustAET", info.GameExecutableName);
            Assert.Equal(string.Empty, info.RealGameExecutableName);
            Assert.Equal(string.Empty, info.LauncherExecutableName);
            Assert.Equal("libasher_bootstrap.so", info.BootstrapLibraryName);
            Assert.False(info.UsesLauncherSwap);
            Assert.False(info.SupportsRecoveryHelper);
            Assert.False(info.WritesPortableSettings);
        }

        [Fact]
        public void Settings_directory_uses_xdg_config_home()
        {
            var original = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

            try
            {
                Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", "/xdg-test");

                Assert.Equal(
                    Path.Combine("/xdg-test", "Asher"),
                    new LinuxPlatformInfo().UserSettingsDirectory);
            }
            finally
            {
                Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", original);
            }
        }

        [Fact]
        public void Settings_directory_falls_back_to_home_config()
        {
            var originalXdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            var originalHome = Environment.GetEnvironmentVariable("HOME");

            try
            {
                Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
                Environment.SetEnvironmentVariable("HOME", "/home/tester");

                Assert.Equal(
                    Path.Combine("/home/tester", ".config", "Asher"),
                    new LinuxPlatformInfo().UserSettingsDirectory);
            }
            finally
            {
                Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", originalXdg);
                Environment.SetEnvironmentVariable("HOME", originalHome);
            }
        }
    }
}
