namespace Asher.Core.Models
{
    public class PatchProgress
    {
        public string CurrentOperation { get; set; } = string.Empty;
        public int Current { get; set; }
        public int Total { get; set; }
        public bool IsIndeterminate { get; set; }
    }
}