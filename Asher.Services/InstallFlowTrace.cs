namespace Asher.Services
{
    /// <summary>
    /// Temporary verbose tracing for install/uninstall/state flows (stderr → Electron diagnostic log).
    /// </summary>
    internal static class InstallFlowTrace
    {
        public static void Log(string step, string? details = null)
        {
            var line = details == null
                ? $"[ASHER-TRACE] {step}"
                : $"[ASHER-TRACE] {step} | {details}";
            Console.Error.WriteLine(line);
        }
    }
}
