namespace Asher.Core.Models
{
    /// <summary>
    /// Explicit installation state, replacing inference from backup presence.
    /// </summary>
    public enum InstallationState
    {
        NotInstalled,
        Installed,
        Partial
    }
}
