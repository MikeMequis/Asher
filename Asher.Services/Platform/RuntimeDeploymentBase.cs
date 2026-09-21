using Asher.Core;
using Asher.Services.Interfaces;
using System.Reflection;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Shared runtime/mod deployment logic. Subclasses only supply the platform's file set
    /// (Linux additionally ships libasher_bootstrap.so), so the deployment rules are not
    /// duplicated.
    /// </summary>
    public abstract class RuntimeDeploymentBase : IRuntimeDeployment
    {
        public abstract IReadOnlyList<string> RequiredRuntimeFiles { get; }

        public abstract IReadOnlyList<string> ManagedRuntimeFiles { get; }

        public abstract IReadOnlyList<string> DefaultModFiles { get; }

        public abstract IReadOnlyList<string> DefaultModsSourceFolderNames { get; }

        public bool HasActiveRuntime(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return false;

            return RequiredRuntimeFiles.Any(fileName =>
                File.Exists(Path.Combine(asherFolder, fileName)));
        }

        public bool HasManagedRuntime(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return false;

            return ManagedRuntimeFiles.Any(fileName =>
                File.Exists(Path.Combine(asherFolder, fileName)));
        }

        public virtual bool IsRuntimeInstalled(string gameFolderPath) =>
            HasManagedRuntime(gameFolderPath);

        public virtual void DeployRuntimeFiles(string gameFolderPath, string sourceFolder)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            Directory.CreateDirectory(asherFolder);

            foreach (var fileName in RequiredRuntimeFiles)
            {
                var sourcePath = Path.Combine(sourceFolder, fileName);
                var destPath = Path.Combine(asherFolder, fileName);

                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException($"Arquivo necessário não encontrado: {fileName}", sourcePath);

                File.Copy(sourcePath, destPath, overwrite: true);
            }

            ValidateHarmonyAssembly(Path.Combine(asherFolder, "0Harmony.dll"));
        }

        public int DeployDefaultMods(string gameFolderPath, IEnumerable<string> defaultModsSourceCandidates)
        {
            var modsFolder = AsherPaths.GetModsFolderPath(gameFolderPath);
            Directory.CreateDirectory(modsFolder);

            var copied = 0;
            foreach (var sourceFolder in defaultModsSourceCandidates)
            {
                if (!Directory.Exists(sourceFolder))
                    continue;

                foreach (var fileName in DefaultModFiles)
                {
                    var sourcePath = Path.Combine(sourceFolder, fileName);
                    if (!File.Exists(sourcePath))
                        continue;

                    File.Copy(sourcePath, Path.Combine(modsFolder, fileName), overwrite: true);
                    copied++;
                }

                if (copied > 0)
                    return copied;
            }

            return copied;
        }

        public void CleanRuntimeFiles(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            if (!Directory.Exists(asherFolder))
                return;

            foreach (var file in Directory.GetFiles(asherFolder))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Best effort — locked files must not abort uninstall.
                }
            }

            foreach (var directory in Directory.GetDirectories(asherFolder))
            {
                var directoryName = Path.GetFileName(directory);
                if (ShouldPreserveAsherSubfolder(directoryName))
                    continue;

                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {
                    // Best effort — e.g. file locks under Mods/.
                }
            }

            // Legacy WPF layout (game/Asher.App).
            var legacyManagerPath = Path.Combine(gameFolderPath, AsherPaths.ManagerFolderName);
            if (Directory.Exists(legacyManagerPath))
            {
                try
                {
                    Directory.Delete(legacyManagerPath, true);
                }
                catch
                {
                    // Ignore — may be locked or absent after migration.
                }
            }
        }

        /// <summary>
        /// Subfolders kept across UI uninstall (backup for safety, logs for diagnostics).
        /// The manager UI is Distribution-only — do not keep Asher.App.
        /// </summary>
        private static bool ShouldPreserveAsherSubfolder(string directoryName) =>
            directoryName.Equals(AsherPaths.BackupFolderName, StringComparison.OrdinalIgnoreCase)
            || directoryName.Equals(AsherPaths.LogsFolderName, StringComparison.OrdinalIgnoreCase);

        private static void ValidateHarmonyAssembly(string harmonyPath)
        {
            try
            {
                foreach (var reference in Assembly.ReflectionOnlyLoadFrom(harmonyPath).GetReferencedAssemblies())
                {
                    if (reference.Name == "System.Runtime" && reference.Version.Major >= 5)
                    {
                        throw new InvalidOperationException(
                            "0Harmony.dll incompatível: foi copiada de um target .NET moderno (net8/net10). " +
                            "Use a versão net472 de packages\\Lib.Harmony.2.4.2\\lib\\net472.");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // Best-effort validation only.
            }
        }
    }
}
