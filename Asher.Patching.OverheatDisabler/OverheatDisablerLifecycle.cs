using Asher.SDK.Patching.Core;

namespace Asher.Patching.OverheatDisabler
{
    /// <summary>
    /// Monitor de lifecycle para Dust Storm Overheat Disabler.
    /// </summary>
    public sealed class OverheatDisablerLifecycle : BaseAsherLifecycle
    {
        public override string Name => "Dust Storm Overheat Disabler";
        protected override bool EnableAutoLogging => false;
    }
}
