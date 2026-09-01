namespace Asher.Core.Models
{
    /// <summary>
    /// Informações sobre a pasta do jogo detectada
    /// </summary>
    public class GameFolderInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
