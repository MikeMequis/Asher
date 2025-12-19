namespace Asher.Core.Models
{
    public class GameFolderInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string PatchesFolderPath { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool HasPatchesFolder { get; set; }
    }
}