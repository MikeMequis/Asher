namespace Asher.Services.Platform
{
    /// <summary>
    /// Contents of <c>Asher/install.json</c> on Linux. Only fields needed to validate the
    /// installed payload are persisted.
    /// </summary>
    internal sealed class LinuxInstallManifest
    {
        public int SchemaVersion { get; set; }

        public string? InstalledAtUtc { get; set; }

        public string? PayloadVersion { get; set; }

        public string? BootstrapArchitecture { get; set; }

        public string? GameArchitecture { get; set; }
    }
}
