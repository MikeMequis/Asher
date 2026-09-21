namespace Asher.Core.Models
{
    /// <summary>
    /// Installation state plus the capabilities the frontend needs, independent of the
    /// platform-specific meaning of "backup".
    /// </summary>
    public sealed class InstallStateInfo
    {
        public InstallationState State { get; set; } = InstallationState.NotInstalled;

        /// <summary>An Asher installation exists and can be removed.</summary>
        public bool CanUninstall { get; set; }

        /// <summary>The platform can restore original game files (launcher-swap backup).</summary>
        public bool CanRestore { get; set; }

        /// <summary>Primary marker/artifact found, or empty when not installed.</summary>
        public string Marker { get; set; } = string.Empty;
    }
}
