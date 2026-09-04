namespace Asher.Services.Application.Contracts
{
    public sealed class GameFolderDto
    {
        public string Path { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
