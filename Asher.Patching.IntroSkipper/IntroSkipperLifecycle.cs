using Asher.SDK.Patching.Core;

namespace Asher.Patching.IntroSkipper
{
    /// <summary>
    /// Monitor de lifecycle para Intro Skipper.
    /// </summary>
    public sealed class IntroSkipperLifecycle : BaseAsherLifecycle
    {
        public override string Name => "Intro Skipper";
        protected override bool EnableAutoLogging => false;
    }
}