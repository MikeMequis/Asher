using Asher.Core;
using Asher.Core.Platform;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Linux runtime payload: managed runtime + Harmony + the native libasher_bootstrap.so
    /// entry library. Deployment is idempotent and writes Asher/install.json after a complete copy.
    /// </summary>
    public sealed class LinuxRuntimeDeployment : RuntimeDeploymentBase
    {
        public const string ManifestFileName = "install.json";

        private const int ManifestSchemaVersion = 1;
        private const string BootstrapArchitectureMismatchMessage =
            "Asher bootstrap architecture does not match the game executable";

        private static readonly JsonSerializerOptions ManifestOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        private readonly IPlatformInfo _platform;

        public LinuxRuntimeDeployment(IPlatformInfo platform)
        {
            _platform = platform;
            RequiredRuntimeFiles = new[]
            {
                "Asher.Runtime.dll",
                "Asher.SDK.dll",
                "0Harmony.dll",
                platform.BootstrapLibraryName
            };
        }

        public override IReadOnlyList<string> RequiredRuntimeFiles { get; }

        public override IReadOnlyList<string> ManagedRuntimeFiles { get; } = new[]
        {
            "Asher.Runtime.dll",
            "Asher.SDK.dll",
            "0Harmony.dll"
        };

        public override IReadOnlyList<string> DefaultModFiles { get; } = new[]
        {
            "Asher.Patching.DebugEnabler.dll",
            "Asher.Patching.IntroSkipper.dll",
            "Asher.Patching.GraphicsDeprofiler.dll",
            "Asher.Patching.MuteVoiceActing.dll",
            "Asher.Patching.OverheatDisabler.dll"
        };

        public override IReadOnlyList<string> DefaultModsSourceFolderNames { get; } = new[]
        {
            AsherPaths.DefaultModsFolderName,
            AsherPaths.ModsFolderName
        };

        public static string GetManifestPath(string gameFolderPath) =>
            Path.Combine(AsherPaths.GetRuntimeFolderPath(gameFolderPath), ManifestFileName);

        public override void DeployRuntimeFiles(string gameFolderPath, string sourceFolder)
        {
            base.DeployRuntimeFiles(gameFolderPath, sourceFolder);
            WriteManifest(gameFolderPath);
        }

        public override bool IsRuntimeInstalled(string gameFolderPath)
        {
            if (!File.Exists(GetManifestPath(gameFolderPath)))
                return false;

            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            return RequiredRuntimeFiles.All(fileName =>
                File.Exists(Path.Combine(asherFolder, fileName)));
        }

        private void WriteManifest(string gameFolderPath)
        {
            var asherFolder = AsherPaths.GetRuntimeFolderPath(gameFolderPath);
            var bootstrapPath = Path.Combine(asherFolder, _platform.BootstrapLibraryName);
            var gameExecutablePath = Path.Combine(gameFolderPath, _platform.GameExecutableName);

            var bootstrapArchitecture = ElfArchitecture.TryRead(bootstrapPath);
            var gameArchitecture = ElfArchitecture.TryRead(gameExecutablePath);

            if (bootstrapArchitecture != null && gameArchitecture != null
                && !string.Equals(bootstrapArchitecture, gameArchitecture, StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    $"[asher] {BootstrapArchitectureMismatchMessage}: bootstrap={bootstrapArchitecture} game={gameArchitecture}");
            }

            var manifest = new LinuxInstallManifest
            {
                SchemaVersion = ManifestSchemaVersion,
                InstalledAtUtc = DateTime.UtcNow.ToString("o"),
                PayloadVersion = TryReadPayloadVersion(Path.Combine(asherFolder, "Asher.Runtime.dll")),
                BootstrapArchitecture = bootstrapArchitecture,
                GameArchitecture = gameArchitecture
            };

            File.WriteAllText(
                GetManifestPath(gameFolderPath),
                JsonSerializer.Serialize(manifest, ManifestOptions));
        }

        private static string? TryReadPayloadVersion(string runtimeAssemblyPath)
        {
            try
            {
                if (!File.Exists(runtimeAssemblyPath))
                    return null;

                var version = FileVersionInfo.GetVersionInfo(runtimeAssemblyPath).FileVersion;
                return string.IsNullOrWhiteSpace(version) ? null : version;
            }
            catch
            {
                return null;
            }
        }
    }
}
