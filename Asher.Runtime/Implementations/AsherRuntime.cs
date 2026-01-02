using Asher.Runtime.Interfaces;
using System;
using System.IO;

namespace Asher.Runtime.Implementations
{
    public class AsherRuntime : IAsherRuntime
    {
        public void Initialize(RuntimeContext context)
        {
            try
            {
                if (!Directory.Exists(context.LogPath))
                    Directory.CreateDirectory(context.LogPath);

                var logFile = Path.Combine(
                    context.LogPath,
                    "runtime.log"
                );

                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Asher] Runtime inicializado com sucesso.\n"
                );

                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Asher] GamePath: {context.GamePath}\n"
                );
                
                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Asher] LauncherPath: {context.LauncherPath}\n"
                );
            }
            catch (Exception ex)
            {
                // Fallback log if log path is unavailable
                File.AppendAllText(
                    "asher_runtime_fatal.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR IN RUNTIME: {ex}\n"
                );
            }
        }
    }
}
