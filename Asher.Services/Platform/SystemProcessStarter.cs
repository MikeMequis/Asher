using Asher.Services.Interfaces;
using System.Diagnostics;

namespace Asher.Services.Platform
{
    public sealed class SystemProcessStarter : IProcessStarter
    {
        public void Start(ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo);
            if (process == null)
                return;

            if (startInfo.RedirectStandardOutput)
                _ = Task.Run(() => PumpAsync(process.StandardOutput, "game-stdout"));

            if (startInfo.RedirectStandardError)
                _ = Task.Run(() => PumpAsync(process.StandardError, "game-stderr"));
        }

        private static async Task PumpAsync(StreamReader reader, string source)
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                    Console.Error.WriteLine($"[{source}] {line}");
            }
            catch
            {
                // The child stream or the Host stderr sink closed; nothing else to drain.
            }
        }
    }
}
