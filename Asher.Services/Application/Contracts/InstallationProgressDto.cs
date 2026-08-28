namespace Asher.Services.Application.Contracts
{
    public sealed class InstallationProgressDto
    {
        public double Percentage { get; init; }
        public string Message { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
    }
}
