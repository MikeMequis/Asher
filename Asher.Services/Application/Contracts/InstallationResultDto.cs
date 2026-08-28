namespace Asher.Services.Application.Contracts
{
    public sealed class InstallationResultDto
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string GameFolderPath { get; init; } = string.Empty;
        public string? ErrorMessage { get; init; }
    }
}
