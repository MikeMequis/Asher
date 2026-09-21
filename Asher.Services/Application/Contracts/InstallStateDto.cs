namespace Asher.Services.Application.Contracts
{
    /// <summary>
    /// Explicit installation state and capabilities for the frontend.
    /// <see cref="State"/> is serialized camelCase: notInstalled | installed | partial.
    /// </summary>
    public sealed class InstallStateDto
    {
        public string State { get; set; } = "notInstalled";
        public bool CanUninstall { get; set; }
        public bool CanRestore { get; set; }
        public string Marker { get; set; } = string.Empty;
    }
}
