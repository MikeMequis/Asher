using Asher.SDK.Patching.Core;

namespace Asher.Patching.DebugEnabler
{
    /// <summary>
    /// Monitor de lifecycle para Debug Enabler.
    /// </summary>
    public sealed class DebugEnablerLifecycle : BaseAsherLifecycle
    {
        public override string Name => "Debug Enabler";
        protected override bool EnableAutoLogging => false;
    }
}