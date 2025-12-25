using Asher.Runtime.Interfaces;
using System;
using System.IO;

namespace Asher.Runtime.Implementations
{
    public class AsherRuntime : IAsherRuntime
    {
        public void Initialize(RuntimeContext context)
        {
            Directory.CreateDirectory(context.LogPath);

            var logFile = Path.Combine(
                context.LogPath,
                $"asher_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            );

            File.AppendAllText(
                logFile,
                "[Asher] Runtime inicializado com sucesso.\n"
            );

            File.AppendAllText(
                logFile,
                $"[Asher] GamePath: {context.GamePath}\n"
            );
        }
    }
}
