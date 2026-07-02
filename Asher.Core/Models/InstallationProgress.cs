namespace Asher.Core.Models
{
    /// <summary>
    /// Progresso da instalação
    /// </summary>
    public class InstallationProgress
    {
        public double Percentage { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
