namespace Asher.Models
{
    public class HarmonyPatchInfo : BindableBase
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string AssemblyPath { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public List<string> Conflicts { get; set; } = new();
        public int Priority { get; set; } = 1000;
        public HarmonyPatchType PatchType { get; set; } = HarmonyPatchType.Prefix;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class HarmonyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string TargetMethod { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
    }

    public enum HarmonyPatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer
    }
}
