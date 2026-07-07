using Asher.SDK.Patching.Core;

namespace Asher.Patching.MuteVoiceActing
{
    /// <summary>
    /// Monitor de lifecycle para Voice Acting Muter.
    /// </summary>
    public sealed class MuteVoiceActingLifecycle : BaseAsherLifecycle
    {
        public override string Name => "Voice Acting Muter";
        protected override bool EnableAutoLogging => false;
    }
}
