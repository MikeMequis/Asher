using Asher.SDK.Patching.Core;

namespace Asher.Patching.OverheatDisabler
{
    /// <summary>
    /// Configuração do Dust Storm Overheat Disabler.
    /// </summary>
    public sealed class OverheatDisablerConfig : BaseAsherPatchConfig<OverheatDisablerPatch>
    {
        protected override string PatchName => "Dust Storm Overheat Disabler";
    }
}
