namespace Asher.Core.Platform
{
    /// <summary>
    /// Read-only description of the platform-varying names and locations.
    /// Pure data: behavioral platform logic lives in Asher.Services platform implementations.
    /// </summary>
    public interface IPlatformInfo
    {
        PlatformKind Kind { get; }

        /// <summary>Game executable as shipped by the store (e.g. DustAET.exe / DustAET).</summary>
        string GameExecutableName { get; }

        /// <summary>Renamed original executable used by the launcher-swap model; empty when unused.</summary>
        string RealGameExecutableName { get; }

        /// <summary>Asher launcher executable used by the swap model; empty when unused.</summary>
        string LauncherExecutableName { get; }

        /// <summary>Native bootstrap library (Linux); empty when unused.</summary>
        string BootstrapLibraryName { get; }

        /// <summary>True when installation replaces the game executable with the Asher launcher.</summary>
        bool UsesLauncherSwap { get; }

        /// <summary>True when the platform ships a user-runnable recovery helper script.</summary>
        bool SupportsRecoveryHelper { get; }

        /// <summary>Default game folder name used when scanning known store locations.</summary>
        string DefaultGameFolderName { get; }

        /// <summary>Directory holding the per-user application settings file.</summary>
        string UserSettingsDirectory { get; }

        /// <summary>
        /// True when settings are mirrored next to the application binary (Windows portable behavior).
        /// When false, a <see cref="Asher.Core.AsherPaths.PortableMarkerFileName"/> marker is required.
        /// </summary>
        bool WritesPortableSettings { get; }
    }
}
