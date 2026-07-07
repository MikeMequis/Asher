using Asher.SDK.Patching.Core;

namespace Asher.Patching.MuteVoiceActing
{
    /// <summary>
    /// Configuração do Voice Acting Muter.
    /// </summary>
    public sealed class MuteVoiceActingConfig : BaseAsherPatchConfig<MuteVoiceActingPatch>
    {
        protected override string PatchName => "Voice Acting Muter";
    }
}
