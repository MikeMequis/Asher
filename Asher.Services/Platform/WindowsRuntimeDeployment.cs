using Asher.Core;
using Asher.Services.Interfaces;

namespace Asher.Services.Platform
{
    /// <summary>Windows runtime payload: managed runtime + Harmony + the five default mods.</summary>
    public sealed class WindowsRuntimeDeployment : RuntimeDeploymentBase, IRuntimeDeployment
    {
        public override IReadOnlyList<string> RequiredRuntimeFiles { get; } = new[]
        {
            "Asher.Runtime.dll",
            "Asher.SDK.dll",
            "0Harmony.dll"
        };

        public override IReadOnlyList<string> ManagedRuntimeFiles => RequiredRuntimeFiles;

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
            AsherPaths.DefaultModsFolderName
        };
    }
}
