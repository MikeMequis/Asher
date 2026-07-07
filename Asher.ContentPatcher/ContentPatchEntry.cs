namespace Asher.ContentPatcher
{
    /// <summary>
    /// A single content replacement entry (Stardew Content Patcher-inspired).
    /// </summary>
    public sealed class ContentPatchEntry
    {
        public string Action { get; set; } = "Load";

        /// <summary>
        /// Game content path without extension (e.g. gfx/ui/main_menu).
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Path relative to the patches folder (e.g. assets/main_menu.png).
        /// </summary>
        public string FromFile { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;
    }
}
