using Asher.SDK.Patching.Core;

namespace Asher.Patching.DebugEnabler
{
    /// <summary>
    /// Configuração do Debug Enabler.
    /// </summary>
    public sealed class DebugEnablerConfig : BaseAsherPatchConfig<DebugEnablerPatch>
    {
        protected override string PatchName => "Debug Enabler";
    }
}