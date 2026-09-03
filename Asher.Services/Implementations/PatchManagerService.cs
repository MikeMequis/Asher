using System.Reflection;
using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class PatchManagerService : IPatchManagerService
    {
        private const string AsherModAttributeFullName = "Asher.SDK.Patching.AsherModAttribute";

        private readonly IGameLaunchService _gameLaunchService;

        public PatchManagerService(IGameLaunchService gameLaunchService)
        {
            _gameLaunchService = gameLaunchService;
        }

        public Task<IReadOnlyList<ManagedModInfo>> GetModsAsync()
        {
            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult<IReadOnlyList<ManagedModInfo>>(Array.Empty<ManagedModInfo>());

            var modsFolder = AsherPaths.GetModsFolderPath(gameFolder);
            var disabledFolder = AsherPaths.GetDisabledModsFolderPath(gameFolder);
            Directory.CreateDirectory(modsFolder);
            Directory.CreateDirectory(disabledFolder);

            var mods = new List<ManagedModInfo>();

            foreach (var file in Directory.GetFiles(modsFolder, "*.dll"))
            {
                var fileName = Path.GetFileName(file);
                mods.Add(CreateModInfo(file, fileName, isEnabled: true, gameFolder));
            }

            foreach (var file in Directory.GetFiles(disabledFolder, "*.dll"))
            {
                var fileName = Path.GetFileName(file);
                mods.Add(CreateModInfo(file, fileName, isEnabled: false, gameFolder));
            }

            return Task.FromResult<IReadOnlyList<ManagedModInfo>>(mods.OrderBy(m => m.Name).ToList());
        }

        public Task<bool> SetModEnabledAsync(string modFileName, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(modFileName) ||
                !string.Equals(modFileName, Path.GetFileName(modFileName), StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult(false);

            var enabledPath = Path.Combine(AsherPaths.GetModsFolderPath(gameFolder), modFileName);
            var disabledPath = Path.Combine(AsherPaths.GetDisabledModsFolderPath(gameFolder), modFileName);

            try
            {
                var enabledExists = File.Exists(enabledPath);
                var disabledExists = File.Exists(disabledPath);

                if (!enabledExists && !disabledExists)
                    return Task.FromResult(false);

                if (enabledExists && disabledExists)
                    return Task.FromResult(false);

                if (enabled)
                {
                    if (enabledExists)
                        return Task.FromResult(true);

                    File.Move(disabledPath, enabledPath);
                }
                else
                {
                    if (disabledExists)
                        return Task.FromResult(true);

                    Directory.CreateDirectory(AsherPaths.GetDisabledModsFolderPath(gameFolder));
                    File.Move(enabledPath, disabledPath);
                }

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private static ManagedModInfo CreateModInfo(
            string assemblyPath,
            string fileName,
            bool isEnabled,
            string gameFolder)
        {
            var (name, description) = TryReadModMetadata(assemblyPath, gameFolder);

            return new ManagedModInfo
            {
                FileName = fileName,
                Name = string.IsNullOrWhiteSpace(name)
                    ? Path.GetFileNameWithoutExtension(fileName)
                    : name,
                Description = string.IsNullOrWhiteSpace(description)
                    ? "Custom mod assembly"
                    : description,
                IsEnabled = isEnabled
            };
        }

        /// <summary>
        /// Reads [assembly: AsherMod(...)] via MetadataLoadContext (no code execution).
        /// Falls back to AssemblyTitle / AssemblyDescription when the attribute is absent.
        /// </summary>
        private static (string? Name, string? Description) TryReadModMetadata(
            string assemblyPath,
            string gameFolder)
        {
            try
            {
                var resolverPaths = BuildResolverPaths(assemblyPath, gameFolder);
                var resolver = new PathAssemblyResolver(resolverPaths);

                using var context = new MetadataLoadContext(resolver);
                var assembly = context.LoadFromAssemblyPath(assemblyPath);

                string? name = null;
                string? description = null;

                foreach (var attribute in assembly.GetCustomAttributesData())
                {
                    var typeName = attribute.AttributeType.FullName;

                    if (string.Equals(typeName, AsherModAttributeFullName, StringComparison.Ordinal))
                    {
                        if (attribute.ConstructorArguments.Count >= 1)
                            name = attribute.ConstructorArguments[0].Value as string;

                        if (attribute.ConstructorArguments.Count >= 2)
                            description = attribute.ConstructorArguments[1].Value as string;

                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
                {
                    foreach (var attribute in assembly.GetCustomAttributesData())
                    {
                        var typeName = attribute.AttributeType.FullName;
                        if (typeName == typeof(AssemblyTitleAttribute).FullName &&
                            attribute.ConstructorArguments.Count >= 1 &&
                            string.IsNullOrWhiteSpace(name))
                        {
                            name = attribute.ConstructorArguments[0].Value as string;
                        }
                        else if (typeName == typeof(AssemblyDescriptionAttribute).FullName &&
                                 attribute.ConstructorArguments.Count >= 1 &&
                                 string.IsNullOrWhiteSpace(description))
                        {
                            description = attribute.ConstructorArguments[0].Value as string;
                        }
                    }
                }

                return (name, description);
            }
            catch
            {
                return (null, null);
            }
        }

        private static IEnumerable<string> BuildResolverPaths(string assemblyPath, string gameFolder)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddIfExists(string? path)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    paths.Add(path);
            }

            AddIfExists(assemblyPath);

            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (!string.IsNullOrWhiteSpace(runtimeDir) && Directory.Exists(runtimeDir))
            {
                foreach (var dll in Directory.EnumerateFiles(runtimeDir, "*.dll"))
                    paths.Add(dll);
            }

            // Asher.SDK.dll — needed to resolve AsherModAttribute type metadata
            AddIfExists(Path.Combine(AsherPaths.GetRuntimeFolderPath(gameFolder), "Asher.SDK.dll"));
            AddIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Asher.SDK.dll"));
            AddIfExists(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                AsherPaths.HostInstallPayloadFolderName,
                "Asher.SDK.dll"));

            var assemblyDir = Path.GetDirectoryName(assemblyPath);
            if (!string.IsNullOrWhiteSpace(assemblyDir))
            {
                AddIfExists(Path.Combine(assemblyDir, "Asher.SDK.dll"));
                AddIfExists(Path.Combine(assemblyDir, "0Harmony.dll"));
            }

            return paths;
        }
    }
}
