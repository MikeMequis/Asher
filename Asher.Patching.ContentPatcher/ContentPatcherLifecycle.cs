using Asher.SDK.Patching.Core;

namespace Asher.Patching.ContentPatcher
{
    public sealed class ContentPatcherLifecycle : BaseAsherLifecycle
    {
        public override string Name => "Content Patcher";
        protected override bool EnableAutoLogging => false;
    }
}
