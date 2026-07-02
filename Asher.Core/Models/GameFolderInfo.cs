namespace Asher.Core.Models
{
    /// <summary>
    /// Informações sobre a pasta do jogo detectada
    /// </summary>
    public class GameFolderInfo
    {
        public string Path { get; set; }
        public string Version { get; set; }
        public bool IsValid { get; set; }
        public string Source { get; set; }
        public bool HasPatchesFolder { get; set; }
        public string PatchesFolderPath { get; set; }
    }
}