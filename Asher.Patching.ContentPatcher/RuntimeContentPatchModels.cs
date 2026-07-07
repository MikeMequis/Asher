using System.Collections.Generic;

namespace Asher.Patching.ContentPatcher
{
    internal sealed class RuntimeContentPatchConfig
    {
        public string Format { get; set; } = "1.0.0";
        public List<RuntimeContentPatchEntry> Changes { get; set; } = new List<RuntimeContentPatchEntry>();
    }

    internal sealed class RuntimeContentPatchEntry
    {
        public string Action { get; set; } = "Load";
        public string Target { get; set; } = string.Empty;
        public string FromFile { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }
}
