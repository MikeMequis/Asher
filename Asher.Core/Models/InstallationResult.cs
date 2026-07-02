namespace Asher.Core.Models
{
    /// <summary>
    /// Resultado da instalação
    /// </summary>
    public class InstallationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
    }
}