namespace Asher.Runtime.Core
{
    public sealed class RuntimeResult
    {
        public bool Success { get; }
        public string? ErrorMessage { get; }

        private RuntimeResult(bool success, string? error)
        {
            Success = success;
            ErrorMessage = error;
        }

        public static RuntimeResult Ok()
            => new RuntimeResult(true, null);

        public static RuntimeResult Fail(string error)
            => new RuntimeResult(false, error);
    }
}
