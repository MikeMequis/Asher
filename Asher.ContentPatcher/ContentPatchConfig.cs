using System.Collections.Generic;

namespace Asher.ContentPatcher
{
    public sealed class ContentPatchConfig
    {
        public string Format { get; set; } = ContentPatchStore.CurrentFormat;

        public List<ContentPatchEntry> Changes { get; set; } = new();
    }
}
