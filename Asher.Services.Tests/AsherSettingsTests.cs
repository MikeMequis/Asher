using Asher.Core;
using Asher.Core.Platform;

namespace Asher.Services.Tests;

public class AsherSettingsTests
{
    [Fact]
    public void Windows_always_writes_portable_settings()
    {
        var missingBase = Path.Combine(Path.GetTempPath(), "asher-portable-none");

        Assert.True(AsherSettings.ShouldWritePortableSettings(
            new WindowsPlatformInfo(), missingBase));
    }

    [Fact]
    public void Linux_skips_portable_settings_without_marker()
    {
        using var temp = new TempDirectory();

        Assert.False(AsherSettings.ShouldWritePortableSettings(
            new LinuxPlatformInfo(), temp.Root));
    }

    [Fact]
    public void Linux_writes_portable_settings_with_marker()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(AsherPaths.PortableMarkerFileName, string.Empty);

        Assert.True(AsherSettings.ShouldWritePortableSettings(
            new LinuxPlatformInfo(), temp.Root));
    }
}
