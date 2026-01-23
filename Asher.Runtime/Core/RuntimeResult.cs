using System;

namespace Asher.Runtime.Core
{
    public sealed class RuntimeResult
    {
        public bool Success { get; }
        public string? ErrorMessage { get; }
        public object? Data { get; }

        private RuntimeResult(bool success, string? error, object? data = null)
        {
            Success = success;
            ErrorMessage = error;
            Data = data;
        }

        public static RuntimeResult Ok() => new RuntimeResult(true, null);
        public static RuntimeResult Ok(object data) => new RuntimeResult(true, null, data);
        public static RuntimeResult Fail(string error) => new RuntimeResult(false, error);
        public static RuntimeResult Fail(Exception ex) => new RuntimeResult(false, $"{ex.GetType().Name}: {ex.Message}");
        public T? GetData<T>() where T : class => Data as T;
        public override string ToString() => Success ? "Success" : $"Failed: {ErrorMessage}";
    }
}