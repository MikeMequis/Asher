namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Platform-specific deployment of the Asher runtime/bootstrap and default mods into
    /// the game's Asher/ folder, plus cleanup on uninstall. Windows ships the managed
    /// runtime; Linux additionally ships libasher_bootstrap.so.
    /// </summary>
    public interface IRuntimeDeployment
    {
        IReadOnlyList<string> RequiredRuntimeFiles { get; }

        /// <summary>Managed runtime files only (excludes the native platform marker).</summary>
        IReadOnlyList<string> ManagedRuntimeFiles { get; }

        IReadOnlyList<string> DefaultModFiles { get; }

        /// <summary>Folders (relative to an install source) that may contain the default mods.</summary>
        IReadOnlyList<string> DefaultModsSourceFolderNames { get; }

        bool HasActiveRuntime(string gameFolderPath);

        /// <summary>True when any managed runtime file is deployed.</summary>
        bool HasManagedRuntime(string gameFolderPath);

        /// <summary>
        /// True when the platform considers the runtime deployment complete (sufficient for
        /// an <c>installed</c> state). Windows preserves the legacy "any runtime file" meaning;
        /// Linux requires the manifest and the full bootstrap + managed file set.
        /// </summary>
        bool IsRuntimeInstalled(string gameFolderPath);

        /// <summary>Copies <see cref="RequiredRuntimeFiles"/> from an install source folder.</summary>
        void DeployRuntimeFiles(string gameFolderPath, string sourceFolder);

        /// <summary>Copies default mods from the first candidate that has any; returns the count.</summary>
        int DeployDefaultMods(string gameFolderPath, IEnumerable<string> defaultModsSourceCandidates);

        /// <summary>Removes runtime files/subfolders, preserving backup and logs.</summary>
        void CleanRuntimeFiles(string gameFolderPath);
    }
}
