using Asher.SDK.Patching.Core;

namespace Asher.Patching.ContentPatcher
{
    public sealed class ContentPatcherConfig : BaseAsherPatchConfig<ContentPatcherPatch>
    {
        protected override string PatchName => "Content Patcher";
    }
}
